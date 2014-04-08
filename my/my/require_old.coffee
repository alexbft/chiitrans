(->
    globals = window
    globals.globals = globals

    Status =
        LOADING: 0
        COMPLETED: 1

    modules = {}

    RequireProperties =
        modules: modules
        path: null
        moduleName: null
        find: (name) ->
            @defaults.find.call(this, name)
        getModuleName: ->
            @moduleName ? "(inline)"

    newRequireContext = (options) ->
        res = (args...) ->
            _require res, args
        res.__proto__ = RequireProperties
        if options?
            for k, v of options
                res[k] = v
        res

    RequireProperties.defaults = newRequireContext
        find: (name) ->
            path = @path ? @defaults.path ? ''
            if path.length > 0 and path.charAt(path.length - 1) != '/'
                path += '/'
            "#{path}#{name}.js"

    globals.require = newRequireContext()

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

    getFunctionBody = (fn) ->
        srcWithoutComments = fn.toString().replace(/((\/\/.*$)|(\/\*[\s\S]*?\*\/))/mg,'')
        srcWithoutComments.substring(srcWithoutComments.indexOf('{'), srcWithoutComments.lastIndexOf('}') + 1)

    getModuleSource = (url, cb) ->
        console.log "Loading module #{url}"
        $.ajax
            url: url + "?" + $.now()
            dataType: 'text'
            success: cb
            error: (_, status) ->
                console.error "Error loading module #{url}: #{status}"
                cb null        

    _require = (ctx, args) ->
        #console.debug "require: #{ctx.getModuleName()}", args
        if args.length == 0
            func = ->
        else
            func = args.pop()
        if typeof(func) != "function"
            args.push func
            func = ->
        paramNames = getParamNames(func)
        if args.length == 0
            args = paramNames
        args = mapStructure args, (it) -> ctx.find(it)
        if ctx.moduleName?
            #internal require
            argMap = {}
            for arg, i in args
                if not Array.isArray(arg) and paramNames[i]?
                    argMap[arg] = paramNames[i]            
            internalRequire ctx, args, argMap, func
        else
            loadModules ctx, args, func

    loadModules = (ctx, args, func) ->
        names = getUniqueNames args
        dict = {}
        counter = names.length
        done = ->
            res = mapStructure args, (it) => dict[it]
            func res...
            return
        for name in names
            do (name) ->
                loadModule name, (m) ->
                    dict[name] = m
                    counter -= 1
                    if counter <= 0
                        done()
        return

    internalRequire = (ctx, args, argMap, func) ->
        for name, argName of argMap
            ctx.loadingData.argMap[name] = argName
        ctx.loadingData.requires.push [args, func]
        names = getUniqueNames args
        for name in names
            ctx.loadingData.dependencies[name] = true
        return

    loadModuleInContext = (name, context, cb) ->
        if context.loading[name]?
            cb()
        else
            if modules[name]?
                if modules[name].status == Status.LOADING
                    modules[name].callbacks.push cb
                else
                    cb()
            else
                modules[name] =
                    status: Status.LOADING
                    module: null
                    callbacks: []
                context.moduleNames.push name
                context.loading[name] =
                    requires: []
                loadingData =
                    requires: context.loading[name].requires
                    argMap: context.argMap
                    dependencies: {}
                getModuleSource name, (src) ->
                    if src?
                        wrapper = new Function 'require', src
                        wrapper newRequireContext moduleName: name, loadingData: loadingData
                    loadingData.dependencies = Object.keys loadingData.dependencies
                    counter = loadingData.dependencies.length
                    if counter == 0
                        cb()
                    else
                        for depName in loadingData.dependencies
                            loadModuleInContext depName, context, ->
                                counter -= 1
                                if counter <= 0
                                    cb()
                    return
        return

    runModule = (name, fn) ->
        try
            fn()
        catch e
            console.error "Module #{name}: #{e.message}\n#{e.stack}", e
            null

    loadModule = (name, cb) ->
        context =
            loading: {}
            moduleNames: []
            argMap: {}
        loadModuleInContext name, context, ->
            if context.moduleNames.length > 0
                context.moduleNames.reverse()
                loader = []
                _id = 0
                for name in context.moduleNames
                    data = context.loading[name]
                    varName = context.argMap[name]
                    if not varName?
                        _id += 1
                        varName = "_module" + _id
                        context.argMap[name] = varName
                    if data.requires.length == 0
                        loader.push "var #{varName} = null;\n"
                        continue
                    header = "var #{varName} = _exports.#{varName} = _runModule('#{name}', function(){\nvar #{varName} = "
                    body = []
                    for [args, func] in data.requires
                        params = getParamNames(func)
                        fixedParams = []
                        fixedValues = []
                        for arg, i in args
                            paramName = params[i]
                            if paramName?
                                if Array.isArray(arg)
                                    fixedParams.push paramName
                                    fixedValues.push JSON.stringify(mapStructure(arg, (it) => "modules['#{it}'].module")).replace(/\"/g, "")
                                else if not (arg in context.moduleNames)
                                    fixedParams.push paramName
                                    fixedValues.push "modules['#{arg}'].module"
                                else if paramName != context.argMap[arg]
                                    fixedParams.push paramName
                                    fixedValues.push context.argMap[arg]
                        body.push "(function(#{fixedParams.join(', ')})#{getFunctionBody(func)})(#{fixedValues.join(', ')});\n"
                    footer = "return #{varName};\n});"
                    loader.push header + body.join('') + footer
                #console.debug loader.join('\n')
                loaderFunc = new Function 'modules', '_exports', '_runModule', loader.join('\n')
                #console.debug loaderFunc.toString()
                exports = {}
                loaderFunc(modules, exports, runModule)
                for name in context.moduleNames
                    modules[name].status = Status.COMPLETED
                    modules[name].module = exports[context.argMap[name]]
                    for _cb in modules[name].callbacks
                        _cb(modules[name].module)
                    delete modules[name].callbacks
            cb(modules[name].module) if cb?
            return
        return

)()