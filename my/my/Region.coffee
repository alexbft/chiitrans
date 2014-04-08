require (geom) ->
    class Region
        constructor: (@stage, @x, @y, @w, @h) ->
            if @x < 0
                @w += @x
                @x = 0
            if @y < 0
                @h += @y
                @y = 0
            if @x + @w > @stage.w
                @w = @stage.w - @x
            if @y + @h > @stage.h
                @h = @stage.h - @y
            if @w < 0
                @w = 0
            if @h < 0
                @h = 0

        at: (p) ->
            if @isInside p
                @stage.at p
            else
                null

        loc: (p) ->
            if @isInside p
                @stage.loc p
            else
                null

        isInside: (p) ->
            @x <= p.x < @x + @w and @y <= p.y < @y + @h

        randomLocation: ->
            @loc pt randomBetween(@x, @x + @w - 1), randomBetween(@y, @y + @h - 1)

        randomLocationWhere: (predicate) ->
            if @w * @h > 50
                for i in [0...50]
                    res = @randomLocation()
                    if predicate res
                        return res
            randomFrom @locationsWhere predicate

        iter: (fn) ->
            for y in [@y ... @y + @h]
                for x in [@x ... @x + @w]
                    fn @loc pt x, y
            return

        locations: ->
            res = []
            for y in [@y ... @y + @h]
                for x in [@x ... @x + @w]
                    res.push @loc pt x, y
            res

        locationsWhere: (predicate) ->
            res = []
            for y in [@y ... @y + @h]
                for x in [@x ... @x + @w]
                    loc = @loc pt x, y
                    if predicate loc
                        res.push loc
            res

        insideArea: (margin = 1) ->
            new Region @stage, @x + margin, @y + margin, @w - margin * 2, @h - margin * 2

        checkConnectivity: ->
            totalPassable = 0
            @iter (l) ->
                if l.cell().isPassable()
                    totalPassable += 1
            visited = 0
            @bfs @randomLocationWhere((l) -> l.cell().isPassable()), (l) ->
                if l.cell().isPassable()
                    visited += 1
                    true
                else
                    false
            visited == totalPassable

        key: (p) ->
            (p.y - @y) * @w + (p.x - @x)

        bfs: (start, func) ->
            been = []
            been[@key start] = true
            queue = [start]
            while queue.length > 0
                cur = queue.shift()
                if func(@loc cur)
                    for y in [cur.y-1 .. cur.y+1]
                        for x in [cur.x-1 .. cur.x+1]
                            p = pt x, y
                            if @isInside(p) and not been[@key p]
                                been[@key p] = true
                                queue.push p
            return

        toString: ->
            rows = for y in [@y ... @y + @h]
                columns = for x in [@x ... @x + @w]
                    @at(pt x, y).toString()
                columns.join ''
            rows.join '\n'