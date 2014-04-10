require (geom, Tiles, drag, Command) ->
    tw = 32
    th = 32

    iw = 35
    ih = 35

    tiles = new Tiles
        tw: tw
        th: th
        path: 'res/tiles'

    class InventoryView
        tw: 32
        th: 32
        backpackWidth: 8

        constructor: (@el, @inv, @ctl) ->
            @cont = $("""<div class="inventoryView"></div>""").appendTo @el
            @eq = getCanvasContext $("""<canvas class="equip" width="#{2 * iw}" height="#{4 * ih}"></canvas>""").appendTo @cont
            @eq.tiles = tiles
            w = @backpackWidth
            h = Math.ceil @inv.maxItems / w
            @back = getCanvasContext $("""<canvas class="backpack" width="#{w * iw}" height="#{h * ih}"></canvas>""").appendTo @cont
            @back.tiles = tiles
            rightColumn = $("<div>").appendTo @cont
            @floor = getCanvasContext $("""<canvas class="invFloor" width="#{2 * iw}" height="#{2 * ih}"></canvas>""").appendTo rightColumn
            @floor.tiles = tiles
            @trash = $("""<div class="invTrash" style="width:#{2 * iw}px;height:#{2 * ih}px"></div>""").appendTo rightColumn
            @tile = {}
            @redraw()
            @inv.on 'change', => @redraw()

            @proxy = $("""<canvas width="#{tw}" height="#{th}" class="dragProxy" style="display:none"></canvas>""").appendTo $('body')
            @proxyCtx = getCanvasContext @proxy
            @proxyCtx.tiles = tiles

            ofsx = ofsy = null
            dragId = null
            backCanvas = $(@back.canvas)
            backCanvas.dragging
                start: (e, sx, sy) =>
                    [sx, sy] = @getRelativeCoords backCanvas, sx, sy
                    dragId = @getBackpackSlotFromView sx, sy
                    it = @inv.items[dragId]
                    if it?
                        [xx, yy] = @posFromView sx, sy
                        @back.clearRect xx * iw, yy * ih, iw, ih
                        ofsx = sx % iw - 1
                        ofsy = sy % ih - 1
                        @proxyCtx.clear()
                        @proxyCtx.drawTile @getTile(it), 0, 0
                        @proxy.show()
                    else
                        false
                click: (e, sx, sy) =>
                    [sx, sy] = @getRelativeCoords backCanvas, sx, sy
                    slot = @getBackpackSlotFromView sx, sy
                    it = @inv.items[slot]
                    if it?
                        @ctl.register Command.USE, it: it
                drag: (e) =>
                    @proxy.css x: e.pageX - ofsx, y: e.pageY - ofsy
                end: (e) =>
                    {pageX: x, pageY: y} = e
                    if @isPointInside backCanvas, x, y
                        [x, y] = @getRelativeCoords backCanvas, x, y
                        newId = @getBackpackSlotFromView x, y
                        if 0 <= newId < @inv.maxItems
                            tmp = @inv.items[newId]
                            @inv.items[newId] = @inv.items[dragId]
                            @inv.items[dragId] = tmp
                    else if @isPointInside floorCanvas, x, y
                        @ctl.register Command.GRABTO, slot: dragId
                    else if @isPointInside @trash, x, y
                        @inv.remove @inv.items[dragId]
                    @redrawBackpack()
                    @proxy.hide()
            floorCanvas = $(@floor.canvas)
            floorCanvas.dragging
                start: =>
                    if @floorItem?
                        @floor.clear()
                        ofsx = tw / 2
                        ofsy = th / 2
                        @proxyCtx.clear()
                        @proxyCtx.drawTile @getTile(@floorItem), 0, 0
                        @proxy.show()
                    else
                        false
                click: =>
                    if @floorItem?
                        @ctl.register Command.GRAB
                drag: (e) =>
                    @proxy.css x: e.pageX - ofsx, y: e.pageY - ofsy
                end: (e) =>
                    @proxy.hide()
                    {pageX: x, pageY: y} = e
                    if @isPointInside backCanvas, x, y
                        [x, y] = @getRelativeCoords backCanvas, x, y
                        newId = @getBackpackSlotFromView x, y
                        if 0 <= newId < @inv.maxItems
                            @ctl.register Command.GRABTO, slot: newId
                            return
                    else if @isPointInside floorCanvas, x, y
                        @ctl.register Command.GRAB
                        return
                    @updateFloor @floorItem

        redraw: ->
            @redrawEquipment()
            @redrawBackpack()

        redrawEquipment: ->
            @eq.clear()
            for slot, it of @inv.equipment.slots
                if it?
                    tile = @getTile it
                    do (slot, tile) =>
                        tiles.onload =>
                            xy = @getSlotPosition slot
                            @eq.drawTile tile, xy.x, xy.y
            return

        redrawBackpack: ->
            @back.clear()
            for it, i in @inv.items
                if it?
                    tile = @getTile it
                    do (i, tile) =>
                        tiles.onload =>
                            xy = @getBackpackPosition i
                            @back.drawTile tile, xy.x, xy.y
            return

        getTile: (it) ->
            @tile[it.glyph] ?= tiles.load (it.glyph + '.png')

        getSlotPosition: (slot) ->
            switch slot
                when 'rightHand' then @itemPos 0, 0
                when 'leftHand' then @itemPos 1, 0
                when 'chest' then @itemPos 0, 1
                when 'head' then @itemPos 1, 1
                when 'boots' then @itemPos 0, 2
                when 'amulet' then @itemPos 1, 2
                when 'ring1' then @itemPos 0, 3
                when 'ring2' then @itemPos 1, 3
                else throw new Error 'getSlotPosition'

        getBackpackPosition: (n) ->
            @itemPos n % @backpackWidth, n / @backpackWidth | 0

        itemPos: (x, y) ->
            pt x * iw + 1, y * ih + 1

        posFromView: (x, y) ->
            [x / iw | 0, y / ih | 0]

        getBackpackSlotFromView: (x, y) ->
            [ix, iy] = @posFromView x, y
            iy * @backpackWidth + ix

        getRelativeCoords: (el, x, y) ->
            {left, top} = el.offset()
            [x - left, y - top]

        isPointInside: (el, x, y) ->
            {left, top} = el.offset()
            w = el.width()
            h = el.height()
            left <= x < left + w and top <= y < top + h

        updateFloor: (it) ->
            if @floorItem?
                @floor.clear()
            @floorItem = it
            if it?
                tile = @getTile it
                tiles.onload =>
                    @floor.drawTile tile, iw - tw / 2, ih - th / 2
            return
