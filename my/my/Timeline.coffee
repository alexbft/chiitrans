require ->
    class Timeline
        constructor: ->
            @time = 0
            @actors = []

        add: (actor) ->
            @actors.push actor
            actor.time = @time

        remove: (actor) ->
            @actors = _.without @actors, actor

        next: ->
            res = @first()
            @time = res.time
            res

        first: ->
            _.min @actors, (a) -> a.time