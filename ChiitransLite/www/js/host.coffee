qid = new Date().getTime()
_host = null
callbacks = {}

initHost = ->
    methods = JSON.parse external.getMethods()
    _host = {}
    for m in methods
        _host[m] = createMethod m

createMethod = (methodName) ->
    (args...) ->
        n = args.length
        if n > 0 and _.isFunction args[n - 1]
            cb = args[n - 1]
            args = args[0 ... n - 1]
            isInline = false
        else
            cb = null
            isInline = true
        query(methodName, args, cb, isInline)

query = (methodName, args, cb, isInline) ->
    if isInline
        resJson = external.query methodName, JSON.stringify(args), 0
        JSON.parse resJson
    else
        queryId = qid++
        callbacks[queryId] = cb
        external.query methodName, JSON.stringify(args), queryId
        true

host = window.host = ->
    if not _host
        initHost()
    _host

hostCallback = window.hostCallback = (queryId, responseJSON) ->
    qq = "" + queryId
    if callbacks[qq]?
        cb = callbacks[qq]
        delete callbacks[qq]
        cb JSON.parse responseJSON
    return

##########

applyTheme = window.applyTheme = (theme) ->
    $('link#currentTheme').remove()
    if theme
        $('head').append("""<link id="currentTheme" rel="stylesheet" href="themes/#{theme}.css" />""")
    return