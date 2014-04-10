require().defaults.path = "my"
require (defines, utils, Game, StageView, PlayerControls, Minimap, InventoryView) ->
    $ ->
        $('#content').html """
            <div id="stageView"></div>
            <div id="info"></div>
        """
        g = new Game()
        g.create()
        #console.log g.map.toString()
        v = new StageView $('#stageView'), g
        c = new PlayerControls g, v
        g.onAction (a) ->
            v.registerAction a
        v.update()
        ready = true
        minimap = new Minimap $('#info'), g.p.stage()
        minimap.update true
        invView = new InventoryView $('#info'), g.p.inventory, c
        handleInput = ->
            if ready
                ready = false
                v.onReady ->
                    ready = true
                    minimap.update()
                    invView.updateFloor g.p.cell().item
                    cmd = c.getLastCommand()
                    if cmd?
                        g.handleInput cmd
                        v.update()
                        handleInput()
            return
        c.onInput handleInput