require (geom, Command) ->
    Keys =
        LU: 36
        UP: 38
        RU: 33
        LEFT: 37
        RIGHT: 39
        LD: 35
        DOWN: 40
        RD: 34
        SPACE: 32
        NUM5: 12
        1: 49
        TAB: 9
        ENTER: 13
        ESC: 27

        Q: 81
        W: 87
        E: 69
        A: 65
        S: 83
        D: 68
        Z: 90
        X: 88
        C: 67

        0: 48
        1: 49
        2: 50
        3: 51
        4: 52
        5: 53
        6: 54
        7: 55
        8: 56
        9: 57

    moveMap = do ->
        _keyMap =
            LU: [-1, -1]
            UP: [0, -1]
            RU: [1, -1]
            LEFT: [-1, 0]
            NUM5: [0, 0]
            RIGHT: [1, 0]
            LD: [-1, 1]
            DOWN: [0, 1]
            RD: [1, 1]

            Q: [-1, -1]
            W: [0, -1]
            E: [1, -1]
            A: [-1, 0]
            S: [0, 1]
            D: [1, 0]
            Z: [-1, 1]
            X: [0, 1]
            C: [1, 1]

            SPACE: [0, 0]

        moveMap = {}
        for k, v of _keyMap
            moveMap[Keys[k]] = v
        moveMap

    castMap = do ->
        castMap = {}
        for i in [1..10]
            castMap[Keys[i % 10]] = i - 1
        castMap

    nearestTarget = (src, targets) ->
        _.min targets, (t) -> src.loc.distance2 t.loc

    class PlayerControls
        constructor: (@game, @view) ->
            @lastCommand = null
            @setNormalMode()
            $ =>
                $(document).keydown (e) =>
                    #console.log "Key: #{e.which}"
                    @keydownHandler e.which

                $(document).keyup (e) =>
                    @keyupHandler e.which

        setNormalMode: ->
            @keydownHandler = @normalKeydownHandler
            @keyupHandler = @normalKeyupHandler

        normalKeydownHandler: (key) ->
            if key of moveMap
                [x, y] = moveMap[key]
                @register Command.MOVE, to: pt x, y
                false
            else if key of castMap
                targets = @game.p.targets 8
                if targets.length
                    target = nearestTarget @game.p, targets
                    @lastTarget = target
                    @view.setTarget target.loc
                    @setTargetingMode()
                false

        normalKeyupHandler: (key) ->
            if key of moveMap and @lastCommand?.id == Command.MOVE
                @lastCommand = null

        setTargetingMode: ->
            @keydownHandler = @targetingKeydownHandler

        targetingKeydownHandler: (key) ->
            switch
                when key of moveMap
                    [dx, dy] = moveMap[key]
                    targets = @game.p.targets 8
                    targets = (t for t in targets when (dx == 0 or sign(t.loc.x - @lastTarget.loc.x) == dx) and (dy == 0 or sign(t.loc.y - @lastTarget.loc.y) == dy))
                    if targets.length
                        target = nearestTarget @game.p, targets
                        @lastTarget = target
                        @view.setTarget target.loc
                    false
                when key == Keys.ENTER
                    @register Command.CAST, target: @lastTarget
                    @view.clearTarget()
                    @setNormalMode()
                    false
                when key == Keys.ESC
                    @view.clearTarget()
                    @setNormalMode()
                    false

        onInput: (cb) ->
            @onInputCallbacks ?= []
            @onInputCallbacks.push cb
            return

        triggerOnInput: ->
            for cb in @onInputCallbacks
                cb()
            return

        register: (commandId, data) ->
            @lastCommand = new Command commandId, data
            @triggerOnInput()

        getLastCommand: ->
            res = @lastCommand
            if @lastCommand?.id != Command.MOVE
                @lastCommand = null
            res



