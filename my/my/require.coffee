do ->
    globals = window
    globals.globals = globals

    Status =
        LOADING: 0
        COMPLETED: 1
        INLINE: 2

    contexts = {}

    endsWith = (s, searchString) ->
        if s.length < searchString.length
            false
        else
            s.substr(s.length - searchString.length) == searchString    

    getCurrentScriptName = ->
        if (typeof document.currentScript != 'undefined')
            res = document.currentScript?.src
        else
            err = null
            try
                throw new Error()
            catch e
                err = e
            lines = err.stack.split '\n'
            line = lines[lines.length - 1]
            if line == ''
                line = lines[lines.length - 2]
            last = line.lastIndexOf(':', line.lastIndexOf(':') - 1)
            first = line.lastIndexOf('(') + 1
            res = line.substring(first, last)
        if not res? or res == '' 
            return "(inline)"
        else
            for k, v of contexts
                if endsWith(res, k)
                    return k
        res

    class RequireContext
        contexts: contexts
        path: null

        constructor: (@name) ->
            @module = {}
            @status = Status.LOADING
            @callbacks = []
            @dependencies = []
            @path = @defaults.path if @defaults?
            @loadingCounter = 1

        find: (name) ->
            @defaults.find.call(this, name)

        onload: (fn) ->
            if @status == Status.LOADING
                @callbacks.push fn
            else
                fn()
            return

        loadStarted: ->
            @loadingCounter += 1

        loadFinished: ->
            @loadingCounter -= 1
            if @loadingCounter <= 0
                #console.log "Finished loading #{@name}"
                @status = Status.COMPLETED
                for cb in @callbacks
                    cb()
                delete @callbacks
                true
            else
                false

        dependsOn: (name) ->
            been = {}
            _rec = (ctx) ->
                for dep in ctx.dependencies
                    if dep == name
                        return true
                for dep in ctx.dependencies
                    if not been[dep]
                        been[dep] = true
                        nextCtx = contexts[dep]
                        if nextCtx?
                            if _rec nextCtx
                                return true
                false
            _rec this

        @current: ->
            name = getCurrentScriptName()
            ctx = contexts[name]
            if not ctx?
                ctx = new RequireContext(name)
                ctx.status = Status.INLINE
                contexts[name] = ctx
            ctx

    RequireContext::defaults = new RequireContext("(defaults)")
    RequireContext::defaults.find = (name) ->
        path = @path ? @defaults.path ? ''
        if path.length > 0 and path.charAt(path.length - 1) != '/'
            path += '/'
        "#{path}#{name}.js"

    getUniqueNames = (arr) ->
        res = {}
        _rec = (arr) ->
            for name in arr
                if Array.isArray(name)
                    _rec(name)
                else
                    res[name] = true
            return
        _rec arr
        Object.keys res

    mapStructure = (arr, fn) ->
        _rec = (arr) ->
            for it in arr
                if Array.isArray(it)
                    _rec it
                else
                    fn(it)
        _rec arr

    getParamNames = (fn) ->
        res = fn.toString()
          .replace(/((\/\/.*$)|(\/\*[\s\S]*?\*\/)|(\s))/mg,'')
          .match(/^function\s*[^\(]*\(\s*([^\)]*)\)/m)[1]
          .split(/,/)
        if res.length == 1 and res[0] == ""
            []
        else
            res

    # getFunctionBody = (fn) ->
    #     srcWithoutComments = fn.toString().replace(/((\/\/.*$)|(\/\*[\s\S]*?\*\/))/mg,'')
    #     srcWithoutComments.substring(srcWithoutComments.indexOf('{'), srcWithoutComments.lastIndexOf('}') + 1)

    globals.require = (args...) ->
        #console.debug "require: #{ctx.getModuleName()}", args
        ctx = RequireContext.current()
        if args.length == 0
            return ctx
        func = args.pop()
        if typeof(func) != "function"
            args.push func
            func = ->
        if args.length == 0
            args = getParamNames(func)
        args = mapStructure args, (it) -> ctx.find(it)
        depNames = getUniqueNames args
        counter = depNames.length
        ctx.loadStarted()
        doneLoading = ->
            values = mapStructure args, (it) -> contexts[it].module
            res = func.apply ctx.module, values
            if res?
                ctx.module = res
            ctx.loadFinished()
        if counter == 0
            doneLoading()
        else
            ctx.dependencies = getUniqueNames ctx.dependencies.concat depNames
            for depName in depNames
                loadModule depName, ctx.name, ->
                    counter -= 1
                    if counter <= 0
                        doneLoading()
        return ctx

    loadModule = (name, parent, cb) ->
        ctx = contexts[name]
        if ctx?
            if ctx.dependsOn parent
                cb()
            else
                ctx.onload cb
        else
            console.log "Loading module #{name} from #{parent}"
            ctx = contexts[name] = new RequireContext name
            ctx.onload cb
            sc = document.createElement "script"
            sc.async = true
            sc.src = name
            sc.onerror = ->
                console.error "FAILED to load module #{name}!"
                ctx.loadFinished()
            sc.onload = ->
                ctx.loadFinished()
            document.documentElement.appendChild sc
        return