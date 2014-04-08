require (geom, Tiles, Action, Events) ->
    class StageView
        @include Events

        tw: 48
        th: 48
        vw: 19
        vh: 15

        constructor: (@el, @game) ->
            @vw2 = @vw / 2 | 0
            @vh2 = @vh / 2 | 0
            @v2 = pt @vw2, @vh2
            @tiles = new Tiles
                path: 'res/tiles'
                tw: @tw
                th: @th
            @tile = {}
            @redMask = {}
            @loadTiles()
            w = @tw * @vw
            h = @th * @vh
            @cont = cont = $("""<div class="stageViewContainer"></div>""").css(width: w, height: h).appendTo @el
            canvas = $("<canvas class=\"stage\" width=#{w} height=#{h}></canvas>").appendTo cont
            @ctx = getCanvasContext canvas
            @ctx.tiles = @tiles
            targetingCanvas = $("<canvas class=\"targeting\" width=#{w} height=#{h}></canvas>").appendTo cont
            @targetingCtx = getCanvasContext targetingCanvas
            mouseCanvas = $("<canvas class=\"mouse\" width=#{w} height=#{h}></canvas>").appendTo cont
            @mouseCtx = getCanvasContext mouseCanvas
            @actionsBuf = []
            @ready = true
            @readyCallbacks = []

        loadTiles: ->
            for tile in ['floor', 'wall', 'door', 'player', 'mob', 'item', 'blood', 'arrow']
                @tile[tile] = @tiles.load "#{tile}.png"
            @tile.arrowLeft = @tiles.flipX @tile.arrow
            @redMask[@tile.player] = @tiles.colorMask @tile.player, 'red'
            @redMask[@tile.mob] = @tiles.colorMask @tile.mob, 'red'
            return

        update: ->
            g = @game
            @ready = false
            @tiles.onload =>
                actions = @actionsBuf
                @actionsBuf = []
                stage = g.p.stage()
                if not @lastStage? or stage != @lastStage
                    @lastStage = stage
                    @lastInfo = []
                @lastVisibleMobIds ?= {}
                mobAnim = {}
                mobActions = {}
                curInfo = []
                particleAnim = []
                getMobPos = (moves, t) =>
                    geom.interpolatePointWeighted t, moves
                getCenter = (t) =>
                    getMobPos playerMoves, t
                getLastInfo = (p) =>
                    key = p.y * stage.w + p.x
                    @lastInfo[key] ?= {}
                getCurInfo = (p) =>
                    key = p.y * stage.w + p.x
                    curInfo[key] ?= {}
                getLastVisibility = (p) =>
                    getLastInfo(p).vis ? 0
                getNewVisibility = (c) =>
                    if not c?
                        return 0
                    switch
                        when c.isVisible() then 1
                        when c.wasVisible() then 0.5
                        else 0
                #console.log JSON.stringify actions
                for act in actions
                    mobId = act.mob.id
                    if (mobId of mobActions) or (mobId of @lastVisibleMobIds) or act.to.cell().isVisible()
                        (mobActions[mobId] ?= []).push act
                for mobId, acts of mobActions
                    mobAnim[mobId] = mob: acts[0].mob, moves: [t: 0, p: acts[0].from], colors: []
                @lastVisibleMobIds = {}
                @center = g.p.loc
                viewArea = @center.adjacentArea(@vw2 + 1, @vh2 + 1)
                viewArea.iter (loc) =>
                    cell = loc.cell()
                    getCurInfo(loc).vis = getNewVisibility(cell)
                    if cell.isVisible() and cell.mob?
                        mob = cell.mob
                        @lastVisibleMobIds[mob.id] = true
                        if not (mob.id of mobAnim)
                            mobAnim[mob.id] = mob: mob, moves: [t: 0, p: mob.loc], colors: []
                n = 1
                for mobId, acts of mobActions
                    start = 0
                    for act in acts
                        if act.id == Action.MOVE
                            mobAnim[mobId].moves.push t: start + 1, p: act.to
                        else if act.id == Action.MELEE
                            mobAnim[mobId].moves.push t: start + 0.7, p: act.from.plus(act.to.minus(act.from).mult(0.5, 0.5))
                            mobAnim[mobId].moves.push t: start + 1, p: act.from
                            targetId = act.target.id
                            if not (targetId of mobAnim)
                                mobAnim[targetId] = mob: act.target, moves: [t: 0, p: act.to], colors: []
                            mobAnim[targetId].colors.push [start, start + 0.7, start + 1]
                        else if act.id == Action.SHOOT
                            from = act.from
                            to = act.target.loc
                            s = start
                            f = s + 1
                            if from.distance(to) > 2
                                start += 1
                                f = s + 2
                            if to.x >= from.x
                                tile = @tile.arrow
                            else
                                tile = @tile.arrowLeft
                            particleAnim.push tile: tile, start: s, finish: f, moves: [{t: s, p: from}, {t: f, p: to}]
                            targetId = act.target.id
                            if not (targetId of mobAnim)
                                mobAnim[targetId] = mob: act.target, moves: [t: 0, p: to], colors: []
                            mobAnim[targetId].colors.push [start, start + 0.7, start + 1]
                        start += 1
                    if n < start
                        n = start

                if g.p.alive
                    playerMoves = mobAnim[g.p.id].moves
                else
                    playerMoves = [t: 0, p: g.p.loc]
                #console.log JSON.stringify playerMoves
                animate ANIM_DURATION * n, (t) =>
                    tt = t * n
                    #console.log t
                    @center = getCenter tt
                    #console.log center.toString()
                    x0 = Math.floor @center.x - @vw2
                    y0 = Math.floor @center.y - @vh2
                    x1 = Math.ceil @center.x + @vw2
                    y1 = Math.ceil @center.y + @vh2
                    drawOpacity = {}
                    for y in [y0 .. y1]
                        for x in [x0 .. x1]
                            p = pt x, y
                            xy = @pointToView p
                            lastVis = getLastVisibility(p)
                            newVis = getCurInfo(p).vis ? 0
                            vis = interpolate t, [lastVis, newVis]
                            if vis > 0
                                cell = stage.at p
                                @drawCellBackground cell, xy, (lastVis == 1 or newVis == 1)
                            if vis < 1
                                drawOpacity[vis] ?= []
                                drawOpacity[vis].push xy
                    for mobId, {mob, moves, colors} of mobAnim
                        if t >= 1 and not mob.alive
                            continue
                        xy = @pointToView getMobPos(moves, tt)
                        @drawMob mob, xy
                        if t < 1
                            for [st, mid, fin] in colors
                                if st <= tt < fin
                                    if tt <= mid
                                        opc = (tt - st) / (mid - st)
                                    else
                                        opc = 1
                                    @ctx.globalAlpha = opc
                                    @ctx.drawTile @redMask[@getMobTile mob], xy.x, xy.y
                                    @ctx.globalAlpha = 1
                    if t < 1
                        for {tile, start, finish, moves} in particleAnim
                            if start <= tt < finish
                                p = geom.interpolatePointWeighted tt, moves
                                xy = @pointToView p
                                @ctx.drawTile tile, xy.x, xy.y
                    for visS, coords of drawOpacity
                        vis = Number visS
                        @ctx.fillStyle = "rgba(0, 0, 0, #{1 - vis})"
                        for xy in coords
                            @ctx.fillRect xy.x, xy.y, @tw, @th
                    @drawHover()
                , =>
                    @lastInfo = curInfo
                    #@center = g.p.loc
                    @initMouseControls()
                    @updateHover()
                    @triggerReady()
                return

        pointToView: (p) ->
            p.minus(@center).plus(@v2).mult(@tw, @th)

        pointFromView: (p) ->
            twInv = 1 / @tw
            thInv = 1 / @th
            p.mult(twInv, thInv).minus(@v2).plus(@center).floor()

        drawCellBackground: (cell, xy, isVisible) ->
            terrain = @getTerrainTile cell.terrain
            @ctx.drawTile terrain, xy.x, xy.y
            if isVisible
                if cell.feature?
                    feat = @getFeatureTile cell.feature
                    @ctx.drawTile feat, xy.x, xy.y                       
                if cell.item?
                    item = @getItemTile cell.item
                    @ctx.drawTile item, xy.x, xy.y

        drawMob: (mob, xy) ->
            mobTile = @getMobTile mob
            @ctx.drawTile mobTile, xy.x, xy.y            

        onReady: (cb) ->
            if @ready
                cb()
            else
                @once 'ready', cb

        triggerReady: ->
            @ready = true
            @trigger 'ready'
            return

        getTerrainTile: (terr) ->
            switch terr
                when Terrain.WALL
                    @tile.wall
                when Terrain.DOOR
                    @tile.door
                else
                    @tile.floor

        getItemTile: ->
            @tile.item

        getMobTile: (mob) ->
            switch mob.glyph
                when '@'
                    @tile.player
                else
                    @tile.mob

        getFeatureTile: ->
            @tile.blood

        registerAction: (action) ->
            @actionsBuf.push action

        setTarget: (p) ->
            xy = @pointToView(p).plus(pt @tw/2, @th/2)
            @targetingCtx.clear()
            @targetingCtx.save()
            @targetingCtx.strokeStyle = '#00ff00'
            @targetingCtx.lineWidth = 2
            @targetingCtx.beginPath()
            @targetingCtx.arc xy.x, xy.y, @tw/2, 0, 2*Math.PI
            @targetingCtx.stroke()
            @targetingCtx.restore()

        clearTarget: ->
            @targetingCtx.clear()

        drawHover: ->
            if @hoverLoc?
                xy = @pointToView @hoverLoc
                @mouseCtx.clear()
                @mouseCtx.strokeRect xy.x + 2, xy.y + 2, @tw - 3, @th - 3
            return

        updateHover: (x, y) ->
            if not x?
                if not @lastMouseCoords?
                    return
                coords = @lastMouseCoords
            else
                coords = pt x, y
                @lastMouseCoords = coords
            p = @pointFromView coords
            if not @lastMouseMove? or not p.eq(@lastMouseMove)
                @lastMouseMove = p
                loc = @game.p.stage().loc(p)
                if loc?
                    @hoverLoc = loc
                    @drawHover()
                    @trigger 'mousemove', loc
                else
                    @mouseCtx.clear()

        initMouseControls: ->
            if not @mouseInitialized
                @mouseInitialized = true
                lastMouseMove = null
                @mouseCtx.strokeStyle = '#00ff00'
                @mouseCtx.lineWidth = 2
                @cont.on 'contextmenu', (e) ->
                    e.preventDefault()
                @cont.mousemove (e) =>
                    @updateHover e.pageX, e.pageY
                    return
                @cont.mousedown (e) =>
                    p = @pointFromView pt e.pageX, e.pageY
                    loc = @game.p.stage().loc(p)
                    if loc?
                        switch
                            when e.which == 1 then @trigger 'mouseleft', loc
                            when e.which == 3 then @trigger 'mouseright', loc
                    e.preventDefault()
                    return
                @cont.mouseout =>
                    @hoverLoc = null
                    @lastMouseMove = null
                    @mouseCtx.clear()
                    @trigger 'mouseout'
            return









