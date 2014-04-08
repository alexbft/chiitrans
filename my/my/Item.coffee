require ->
    class Item
        glyph: '!'

        constructor: (options) ->
            @id = nextId()
            if options?
                for k, v of options
                    @[k] = v            

        toString: ->
            @glyph