require().defaults.path = "my"
require (defines, utils, Game, StageView, PlayerControls) ->
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
        handleInput = ->
            if ready
                ready = false
                v.onReady ->
                    ready = true
                    cmd = c.getLastCommand()
                    if cmd?
                        g.handleInput cmd
                        v.update()
                        handleInput()
            return
        c.onInput handleInput