require (geom) ->
    class Minimap
        tw: 5
        th: 5

        constructor: (@el, @stage) ->
            #w = @stage.w * @tw
            #h = @stage.h * @th
            @sw = @stage.w
            @sh = @stage.h
            @dw = @sw * @tw
            @dh = @sh * @th
            @ctx = getCanvasContext $("""<canvas class="minimap" width="#{@dw}" height="#{@dh}"></canvas>""").
                appendTo @el
            @ctx.imageSmoothingEnabled = false
            @ctx.mozImageSmoothingEnabled = false
            @temp = getCanvasContext $("""<canvas width="#{@sw}" height="#{@sh}"></canvas>""")
            @img = @temp.createImageData @sw, @sh
            @data = @img.data

        update: (redrawAll) ->
            for y in [0 ... @sh]
                for x in [0 ... @sw]
                    cell = @stage.at(pt x, y)
                    if redrawAll or cell.isVisible()
                        @draw x, y, cell
            @temp.putImageData @img, 0, 0
            @ctx.drawImage @temp.canvas, 0, 0, @sw, @sh, 0, 0, @dw, @dh

        draw: (x, y, cell) ->
            [r, g, b] = @getCellColor cell
            i = (y * @sw + x) * 4
            @data[i] = r
            @data[i + 1] = g
            @data[i + 2] = b
            @data[i + 3] = 255
            return

        getCellColor: (cell) ->
            if not cell.isVisible()
                [0, 0, 0]
            else if cell.mob?
                if cell.mob.isPlayer()
                    [0, 255, 0]
                else
                    [255, 0, 0]
            else if cell.item?
                [196, 0, 255]
            else
                switch cell.terrain
                    when Terrain.FLOOR
                        [128, 128, 128]
                    when Terrain.WALL
                        [40, 40, 40]
                    when Terrain.WATER
                        [80, 80, 255]
                    when Terrain.DOOR
                        [160, 96, 0]
                    else
                        throw new Error "WTF?"





            
