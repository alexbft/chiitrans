require (geom) ->
    makeTrail = (region, x, y, len, num) ->
        cur = pt x, y
        for i in [0...num]
            cur = makeCorridor region, cur, len
        return

    makeCorridor = (region, st, len) ->
        randomDir = (z, mz) ->
            ch = (0.5 + z / mz) * 0.5
            if chance ch then -1 else 1

        if coinflip()
            dir = pt 0, randomDir(st.y - region.y, region.h)
        else
            dir = pt randomDir(st.x - region.x, region.w), 0
        #test
        if dir.x == 0
            len = (Math.max region.h / 8, 4) | 0
        else
            len = (Math.max region.w / 8, 4) | 0
        len = randomBetween len / 2 | 0, len
        cur = st
        for i in [0...len]
            next = cur.plus dir
            if not region.isInside next
                break
            if region.isInside(next.plus(dir)) and region.at(next.plus(dir)).terrain == Terrain.FLOOR and not chance INTERSECT_CHANCE
                break
            cur = next
            region.at(cur).terrain = Terrain.FLOOR
        cur

    makeRandomRoom = (region, maxWidth, maxHeight) ->
        isPassage = (p) ->
            if region.at(p)?.terrain in [Terrain.FLOOR, Terrain.DOOR, Terrain.WATER] then 1 else 0
        placeDoorUD = (p) ->
            if region.at(p)?.terrain == Terrain.FLOOR and
                    not isPassage(p.plus(pt -1, 0)) and
                    not isPassage(p.plus(pt 1, 0))
                region.at(p).terrain = Terrain.DOOR
        placeDoorLR = (p) ->
            if region.at(p)?.terrain == Terrain.FLOOR and
                    not isPassage(p.plus(pt 0, -1)) and
                    not isPassage(p.plus(pt 0, 1))
                region.at(p).terrain = Terrain.DOOR

        xd = randomBetween 2, maxWidth
        yd = randomBetween 2, maxHeight
        attempt 10, ->
            xs = region.x + random region.w - xd + 1
            ys = region.y + random region.h - yd + 1
            xf = xs + xd - 1
            yf = ys + yd - 1
            passages = 0
            for x in [xs..xf]
                passages += isPassage pt x, ys-1
                passages += isPassage pt x, yf+1
            for y in [ys..yf]
                passages += isPassage pt xs-1, y
                passages += isPassage pt xf+1, y
            if passages < 1 or passages > (3 + xd + yd) / 4
                return false
            for x in [xs..xf]
                for y in [ys..yf]
                    region.at(pt x, y).terrain = Terrain.FLOOR
            for x in [xs..xf]
                placeDoorUD pt x, ys-1
                placeDoorUD pt x, yf+1
            for y in [ys..yf]
                placeDoorLR pt xs-1, y
                placeDoorLR pt xf+1, y
            #region.at(xs-1,yf+1)?.terrain = Terrain.WATER
            true

    makeTrail: makeTrail
    makeRandomRoom: makeRandomRoom