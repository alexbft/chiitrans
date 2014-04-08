require ->
    class Point
        constructor: (@x, @y) ->

        plus: (p) ->
            new Point @x + p.x, @y + p.y

        minus: (p) ->
            new Point @x - p.x, @y - p.y

        mult: (sx, sy) ->
            new Point @x * sx, @y * sy

        floor: ->
            new Point @x | 0, @y | 0

        distance: (p) ->
            distance @, p

        distance2: (p) ->
            distance2 @, p

        eq: (p) ->
            @x == p.x and @y == p.y

        toString: ->
            "x=#{@x},y=#{@y}"

        toJSON: ->
            x: @x, y: @y

    globals.pt = (x, y) ->
        new Point x, y

    distance = ({x: x0, y: y0}, {x: x1, y: y1}) ->
        x = x1 - x0
        y = y1 - y0
        Math.sqrt x * x + y * y

    distance2 = ({x: x0, y: y0}, {x: x1, y: y1}) ->
        x = x1 - x0
        y = y1 - y0
        x * x + y * y

    raycast = ({x:sx, y:sy}, {x:fx, y:fy}, radius, cb) ->
        # normalize fx and fy
        dx = fx - sx
        dy = fy - sy
        qx = dx / dy
        qy = dy / dx
        if dx != 0
            xx = Math.sqrt(radius * radius / (1 + qy * qy))
            if dx < 0 then xx = -xx
            yy = xx * qy
            fx = sx + xx
            fy = sy + yy
        else # dx == 0 means vertical line
            if dy > 0
                fy = sy + radius
            else
                fy = sy - radius
        # find intersections
        if Math.abs(dx) > Math.abs(dy)
            x0 = if dx > 0 then Math.ceil(sx) else Math.floor(sx)
            x1 = if dx > 0 then Math.floor(fx) else Math.ceil(fx)
            oldx = x0
            oldy = Math.floor sy
            for x in [x0..x1]
                y = sy + (x - sx) * qy
                cellx = if dx > 0 then x else x - 1
                if isInt y
                    celly = if dy > 0 then Math.round(y) else Math.round(y) - 1
                else
                    celly = Math.floor y
                    if celly != oldy
                        if (cb pt oldx, celly) == false then return false
                if (cb pt cellx, celly) == false then return false
                oldx = cellx
                oldy = celly
        else
            y0 = if dy > 0 then Math.ceil(sy) else Math.floor(sy)
            y1 = if dy > 0 then Math.floor(fy) else Math.ceil(fy)
            oldx = Math.floor sx
            oldy = y0
            for y in [y0..y1]
                x = sx + (y - sy) * qx
                celly = if dy > 0 then y else y - 1
                if isInt x
                    cellx = if dx > 0 then Math.round(x) else Math.round(x) - 1
                else
                    cellx = Math.floor x
                    if cellx != oldx
                        if (cb pt cellx, oldy) == false then return false
                if (cb pt cellx, celly) == false then return false
                oldx = cellx
                oldy = celly
        true

    class PointSet extends GenericSet
        key: ({x, y}) ->
            "#{x},#{y}"

    interpolatePoint = (t, points) ->
        xs = (p.x for p in points)
        ys = (p.y for p in points)
        pt interpolate(t, xs), interpolate(t, ys)

    eps = 1e-5
    interpolatePointWeighted = (t, wp) ->
        if wp.length <= 1
            wp[0].p
        else
            cur = 0
            res = wp.length - 1
            for {t: cur}, i in wp
                if t < cur - eps
                    if i == 0
                        return wp[0].p
                    res = i - 1
                    t = (t - old) / (cur - old)
                    break
                old = cur
            if res + 1 >= wp.length
                wp[res].p
            else
                {x: x0, y: y0} = wp[res].p
                {x: x1, y: y1} = wp[res + 1].p
                pt (x0 + (x1 - x0) * t), (y0 + (y1 - y0) * t)

    Point: Point
    distance: distance
    distance2: distance2
    raycast: raycast
    PointSet: PointSet
    interpolatePoint: interpolatePoint
    interpolatePointWeighted: interpolatePointWeighted