require().defaults.path = "my"
require (defines, utils, Tiles) ->
    $('#content').html('<canvas id=c></canvas>')
    c = $('#c')
    ctx = getCanvasContext c
    tiles = new Tiles
        tw: 48
        th: 48
        path: 'res/tiles'
    ctx.tiles = tiles
    fl = tiles.load 'floor.png'
    pl = tiles.load 'player.png'
    tiles.onload ->
        bluePl = tiles.colorMask pl, 'blue'
        redPl = tiles.colorMask pl, 'red'

        ctx.drawTile fl, 0, 0
        ctx.drawTile pl, 0, 0
        ctx.globalAlpha = 0.5
        ctx.drawTile redPl, 0, 0
        ctx.globalAlpha = 0.25
        ctx.drawTile bluePl, 0, 0
