require (Equipment, Events) ->
    class Inventory
        @include Events

        constructor: ->
            @maxItems = 40
            @equipment = new Equipment
            @items = []
            @itemsCount = 0

        isFull: ->
            @itemsCount >= @maxItems

        add: (it) ->
            i = @findEmptySlot()
            if i >= 0
                @items[i] = it
                @itemsCount += 1
                @changed()
            return

        remove: (it) ->
            i = @items.indexOf it
            if i >= 0
                @items[i] = null
                @itemsCount -= 1
                @changed()
            return

        swapItem: (it, id) ->
            if 0 <= id < @maxItems
                res = @items[id]
                @items[id] = it
                @changed()
                res
            else
                it

        findEmptySlot: ->
            for i in [0...@maxItems]
                if not @items[i]?
                    return i
            return -1

        changed: ->
            @trigger 'change'