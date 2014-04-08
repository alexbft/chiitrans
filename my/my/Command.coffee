require ->
    class Command
        @MOVE: 0
        @WAIT: 1
        @CAST: 2
        
        constructor: (@id, data) ->
            if data?
                for k, v of data
                    @[k] = v