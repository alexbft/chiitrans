require (geom, Region) ->
    class Location
        constructor: (@stage, @point) ->
            if @point.point?
                @point = @point.point

        plus: (p) ->
            new Location @stage, @point.plus p

        minus: (p) ->
            new Location @stage, @point.minus p

        mult: (sx, sy) ->
            new Location @stage, @point.mult sx, sy

        distance: (p) ->
            @point.distance p

        distance2: (p) ->
            @point.distance2 p

        eq: (loc) ->
            @stage == loc.stage and @point.eq(loc.point)

        toString: ->
            @point.toString()

        toJSON: ->
            @point.toJSON()

        cell: ->
            @stage.at @point

        adjacentArea: (radiusX = 1, radiusY) ->
            radiusY ?= radiusX
            new Region @stage, @x - radiusX, @y - radiusY, radiusX * 2 + 1, radiusY * 2 + 1

    Object.defineProperties Location::,
        x:
            get: -> @point.x
        y:
            get: -> @point.y

    Location