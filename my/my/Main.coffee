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
        v = new StageView $('#stageView')
        c = new PlayerControls g, v
        g.onAction (a) ->
            v.registerAction a
        v.update g
        ready = true
        handleInput = ->
            if ready
                ready = false
                v.onReady ->
                    ready = true
                    cmd = c.getLastCommand()
                    if cmd?
                        g.handleInput cmd
                        v.update g
                        handleInput()
            return
        c.onInput handleInput