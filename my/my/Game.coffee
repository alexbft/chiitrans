require (Stage, Mob, Item, builder, Command, Action, Timeline, Inventory) ->
    randomMob = ->
        new Mob 
            speed: randomBetween 25, 200
            hp: randomBetween 5, 25
            visibilityRadius: 4

    potion = ->
        new Item
            glyph: 'item'
            use: ({mob}) ->
                mob.heal(25)
                mob.inventory.remove @

    apple = ->
        new Item
            glyph: 'apple'

    class Game
        constructor: ->
            globals.g = @

        create: ->
            @timeline = new Timeline
            attempt 10, =>
                @stage = new Stage 60, 45
                inside = @stage.region.insideArea(1)
                # inside.iterate (c) =>
                #     c.terrain = Terrain.FLOOR
                halfx = inside.w / 2 | 0
                halfy = inside.h / 2 | 0
                for xx in [0..1]
                    for yy in [0..1]
                        x = randomBetween halfx * xx, halfx * (xx + 1) - 1
                        y = randomBetween halfy * yy, halfy * (yy + 1) - 1
                        builder.makeTrail inside, inside.x + x, inside.y + y, 12, 100
                for i in [1..15]
                    builder.makeRandomRoom inside, 10, 7            
                for i in [1..40]
                    mob = randomMob()
                    loc = inside.randomLocationWhere (l) => 
                        l.cell().canEnter mob
                    @createMob loc, mob
                for i in [1..40]
                    loc = inside.randomLocationWhere (l) =>
                        l.cell().terrain == Terrain.FLOOR and not l.cell().item?
                    loc.cell().item = if coinflip() then potion() else apple()
                @p = new Mob
                    glyph: 'player'
                    speed: 100
                    hp: 100
                    visibilityRadius: 10
                    inventory: new Inventory()
                @p.inventory.add potion()
                @p.inventory.add apple()
                startPoint = inside.randomLocationWhere (l) =>
                    l.cell().canEnter @p
                @createMob startPoint, @p
                @stage.region.checkConnectivity()
            @actionsBuf = []
            @updateVisibility()

        createMob: (where, mob) ->
            where.cell().mob = mob
            mob.loc = where
            @timeline.add mob
            mob.onDeath =>
                if mob != @p 
                    @timeline.remove mob
                mob.cell().mob = null
                mob.cell().feature = Feature.BLOODY

        handleInput: (cmd) ->
            if @p.alive
                @doCommand @p, cmd
            else
                @p.wait()
            ctr = 0
            while (next = @timeline.next()) != @p
                if ctr++ > 1000
                    throw new Error "Too many actions in timeline!"
                @doCommand next, @ai next
            @updateVisibility()
            return

        updateVisibility: ->
            @stage.updateVisibility @p.loc, @p.visibilityRadius
            @seeDanger = @stage.seeDanger

        ai: (mob) ->
            newLoc = mob.loc.adjacentArea().randomLocationWhere (l) -> 
                cell = l.cell()
                (cell.mob? and cell.mob != mob) or cell.canEnter mob
            if newLoc?
                new Command Command.MOVE, to: newLoc.point.minus mob.loc
            else
                new Command Command.WAIT

        doCommand: (mob, cmd) ->
            if not cmd?
                return
            switch cmd.id
                when Command.MOVE
                    if cmd.to.eq pt 0, 0
                        mob.wait()
                    else
                        newLoc = mob.loc.plus cmd.to
                        newCell = newLoc.cell()
                        if newCell.mob?
                            @registerAction new Action Action.MELEE, mob: mob, target: newCell.mob, from: mob.loc, to: newLoc
                            mob.attack newCell.mob
                        else if newCell.canEnter mob
                            @registerAction new Action Action.MOVE, mob: mob, from: mob.loc, to: newLoc
                            mob.moveTo newLoc
                when Command.CAST
                    @registerAction new Action Action.SHOOT, mob: mob, target: cmd.target, from: mob.loc, to: cmd.target.loc
                    mob.attack cmd.target
                when Command.WAIT
                    mob.wait()
                when Command.GRAB
                    if mob.cell().item? and not mob.inventory.isFull()
                        mob.inventory.add mob.cell().item
                        mob.cell().item = null
                        mob.time += 50
                when Command.GRABTO
                    it = mob.inventory.swapItem mob.cell().item, cmd.slot
                    mob.cell().item = it
                    mob.time += 50
                when Command.USE
                    if cmd.it.use?
                        cmd.it.use(game: @, mob: mob, target: cmd.target)
                        mob.time += 50
            return

        onAction: (cb) ->
            @onActionCallbacks ?= []
            @onActionCallbacks.push cb

        registerAction: (action) ->
            for cb in @onActionCallbacks
                cb(action)
            return


