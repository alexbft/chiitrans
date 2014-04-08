require (utils) ->
    canvas = (w, h) ->
        res = document.createElement('canvas')
        res.width = w
        res.height = h
        #$('#info').append res
        res

    CanvasExtensions.drawTile = (tile, x, y) ->
        @tiles.drawTile @, tile, x, y

    class Tiles
        tw: 32 # scaled tile width
        th: 32 # scaled tile height
        path: ''

        constructor: (options) ->
            if options?
                for k, v of options
                    @[k] = v
            if @path.length > 0 and not @path.endsWith '/'
                @path += '/'
            @maxId = 0
            @nextId = 0
            @loadCounter = 0
            @callbacks = []
            tempCanvas = canvas @tw, @th
            @tempCtx = getCanvasContext tempCanvas

        allocate: (w, h) ->
            @maxId = w * h
            newCanvas = canvas w * @tw, h * @th
            newCtx = getCanvasContext newCanvas
            if @canvas?
                newCtx.drawImage @canvas, 0, 0
            @canvas = newCanvas
            @ctx = newCtx

        getNextId: ->
            if @nextId >= @maxId
                @allocate (@maxId + 64) / 8, 8
            @nextId++

        pos: (id) ->
            xx = (id / 8 | 0) * @tw
            yy = (id % 8) * @th
            [xx, yy]

        load: (src) ->
            id = @getNextId()
            [xx, yy] = @pos id
            img = new Image
            img.src = @path + src
            img.onload = =>
                @ctx.drawImage img, xx, yy, @tw, @th
                img.onload = null
                img = null
                @_imageLoaded()
                return
            id

        newTile: (drawFunc) ->
            id = @getNextId()
            if drawFunc?
                [xx, yy] = @pos id
                @onload ->
                    drawFunc xx, yy
            @_imageLoaded()
            id

        colorMask: (tile, color) ->
            @newTile (x, y) =>
                @drawTile @ctx, tile, x, y
                @ctx.save()
                @ctx.globalCompositeOperation = 'source-atop'
                @ctx.fillStyle = color
                @ctx.fillRect x, y, @tw, @th
                @ctx.restore()

        flipX: (tile) ->
            @newTile (x, y) =>
                @tempCtx.save()
                @tempCtx.scale -1, 1
                @drawTile @tempCtx, tile, -@tw, 0
                @tempCtx.restore()
                @ctx.drawImage @tempCtx.canvas, x, y

        _imageLoaded: ->
            @loadCounter += 1
            if @loadCounter >= @nextId
                if @callbacks.length > 0
                    callbacks = @callbacks
                    @callbacks = []
                    for cb in callbacks
                        cb()
            return

        onload: (cb) ->
            if @loadCounter >= @nextId
                cb()
            else
                @callbacks.push cb
            return

        drawTile: (ctx, tile, x, y) ->
            [xx, yy] = @pos tile
            ctx.drawImage @canvas, xx, yy, @tw, @th, x, y, @tw, @th