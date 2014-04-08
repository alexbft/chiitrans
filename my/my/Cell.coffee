require ->
    class Cell
        @visibleStamp = 1
        @clearVisibility = ->
            @visibleStamp += 1

        constructor: ->
            @terrain = Terrain.WALL
            @visibleStamp = 0

        canEnter: (mob) ->
            @isPassable() and not @mob?

        isPassable: ->
            @terrain in [Terrain.FLOOR, Terrain.DOOR]

        copy: ->
            res = new Cell
            res.terrain = @terrain if @terrain?
            res.mob = @mob if @mob?
            res

        isOpaque: ->
            @terrain in [Terrain.WALL, Terrain.DOOR]

        hasObstacle: ->
            @isOpaque() or @mob?

        terrainGlyph: ->
            switch @terrain
                when Terrain.WALL then '#'
                when Terrain.DOOR then '+'
                when Terrain.WATER then '~'
                else '.'

        isVisible: ->
            @visibleStamp >= Cell.visibleStamp

        wasVisible: ->
            @visibleStamp > 0

        setVisible: ->
            @visibleStamp = Cell.visibleStamp

        toString: ->
            if @mob?
                @mob.toString()
            else if @item?
                @item.toString()
            else
                @terrainGlyph()