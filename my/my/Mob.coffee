require () ->
    class Mob
        glyph: 'mob'
        alive: true
        kills: 0
        visibilityRadius: 4
        speed: 100
        time: 0
        loc: null
        hp: 1

        constructor: (options) ->
            @id = nextId()
            if options?
                for k, v of options
                    @[k] = v

        speedFactor: ->
            100 / @speed

        stage: ->
            @loc.stage

        cell: ->
            @loc.cell()

        moveTo: (loc) ->
            #action = new Action Action.MOVE, mob: @, from: @loc, to: loc
            d = @loc.distance(loc)
            @cell().mob = null
            @loc = loc
            @cell().mob = @
            @time += 100 * @speedFactor() * d
            #action

        canSee: (where) ->
            @stage().checkLOS @loc, where, @visibilityRadius, (c) -> not c.isOpaque()

        canShoot: (where, radius) ->
            @stage().checkLOS @loc, where, radius, (c) -> not c.hasObstacle()

        attack: (other) ->
            other.hp -= 10
            if other.hp <= 0
                other.die()
                @kills += 1
            @time += 100

        heal: (hp) ->
            @hp += hp
            if @hp > 100
                @hp = 100
            return

        wait: ->
            @time += 100 * @speedFactor()

        die: ->
            if @alive
                @alive = false
                @triggerOnDeath()
            else
                throw new Error "WTF"

        onDeath: (cb) ->
            @onDeathCallbacks ?= []
            @onDeathCallbacks.push cb

        triggerOnDeath: ->
            for cb in @onDeathCallbacks
                cb()
            @onDeathCallbacks = null

        targets: (radius) ->
            res = []
            @loc.adjacentArea(radius).iter (loc) =>
                mob = loc.cell().mob
                if mob? and mob != @ and @canShoot loc, radius
                    res.push mob
            res

        isPlayer: ->
            @glyph == 'player'

        toString: ->
            'A'