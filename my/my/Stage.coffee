require (Cell, Region, Location, geom) ->
    class Stage
        constructor: (@w, @h) ->
            @grid = (new Cell() for i in [0 ... @w * @h])
            @region = new Region @, 0, 0, @w, @h

        at: (p) ->
            if not @isInside p
                null
            else
                @grid[p.y * @w + p.x]

        loc: (p) ->
            if not @isInside p
                null
            else
                new Location @, p

        isInside: (p) ->
            0 <= p.x < @w and 0 <= p.y < @h

        updateVisibility: (sourcePoint, radius) ->
            Cell.clearVisibility()
            go = (pt) ->
                if seen.add pt
                    queue.push pt
                return
            @at(sourcePoint).setVisible()
            start = sourcePoint.plus pt 0.5, 0.5
            border = sourcePoint
            ctr = 0
            seen = new geom.PointSet()
            {x:ox, y:oy} = sourcePoint
            queue = []
            queue.push start.plus pt 1, 0
            while queue.length > 0
                if ctr++ > 1000
                    throw new Error "infloop!"
                    break
                finish = queue.pop()
                geom.raycast start, finish, radius, (p) =>
                    border = p
                    cell = @at p
                    if not cell?
                        false
                    else
                        cell.setVisible()
                        not cell.isOpaque()
                {x:xx, y:yy} = border
                #console.log fx - sx, fy - sy, ':', xx - ox, yy - oy
                if xx > ox and yy <= oy or xx <= ox and yy > oy
                    go pt xx, yy
                if xx < ox and yy <= oy or xx >= ox and yy > oy
                    go pt xx+1, yy
                if xx <= ox and yy < oy or xx > ox and yy >= oy
                    go pt xx, yy+1
                if xx >= ox and yy < oy or xx < ox and yy >= oy
                    go pt xx+1, yy+1
            return

        checkLOS: (start, finish, radius, isPassable) ->
            if start.distance2(finish) > radius * radius
                false
            else
                saw = false
                for i in [0..1]
                    for j in [0..1]
                        geom.raycast start.plus(pt 0.5, 0.5), finish.plus(pt i, j), radius, (p) =>
                            if p.eq finish
                                saw = true
                                false
                            else 
                                c = @at p
                                if not c? or not isPassable c
                                    false
                                else
                                    true
                        if saw then return true
                false

        toString: ->
            @region.toString()