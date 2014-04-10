require ->
    class Item
        glyph: 'item'

        constructor: (options) ->
            @id = nextId()
            if options?
                for k, v of options
                    @[k] = v            

        toString: ->
            '!'