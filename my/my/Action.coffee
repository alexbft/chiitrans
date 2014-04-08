require ->
    class Action
        @MOVE: 0
        @MELEE: 1
        @SHOOT: 2

        constructor: (@id, data) ->
            if data?
                for k, v of data
                    @[k] = v

        toJSON: ->
            res = {}
            for own k, v of @
                res[k] = v.toString()
            res
