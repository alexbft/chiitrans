isSelectWindow = false

$ ->
    #document.ondragover = (e) ->
    #    ev = $.Event e
    #    ev.preventDefault()
    #    return
    #
    #document.ondrop = (e) ->
    #    ev = $.Event e
    #    ev.preventDefault()
    #    dt = e.dataTransfer
    #    if dt.files? and dt.files.length
    #        q = {}
    #        for k, v of dt.files[0]
    #            q[k] = v
    #        alert JSON.stringify q
    #    return

    $(document).keydown (ev) ->
        if ev.keyCode == 27 and isSelectWindow
            ev.preventDefault()
            isSelectWindow = false
            $('#connect_status').empty()
            host().selectWindowClick(isSelectWindow)

    host().getProcesses ({procs, defaultName, defaultPid}) ->
        $pSel = $ '#process'
        for p in procs
            $op = $ """<option value="#{p.id}">#{formatProcessName p.name}</option>"""
            $op.data 'exe', p.name
            $pSel.append $op
        setDefaultProcess defaultPid, defaultName
        return

    $('#select_window').click ->
        isSelectWindow = not isSelectWindow
        if isSelectWindow
            $('#connect_status').html "Select a window to connect to:"
        else
            $('#connect_status').empty()
        host().selectWindowClick(isSelectWindow)

    $('#browse').click ->
        host().browseClick()

    $('.showTranslation').click ->
        host().showTranslationForm()

    $('.options').click ->
        host().showOptions()

    $('#connect').click ->
        sel = $('#process')
        pid = sel.val()
        if pid != ''
            index = sel[0].selectedIndex
            exeName = $(sel[0].options(index)).data('exe')
            $('#connect').prop 'disabled', true
            host().connectClick +pid, exeName, ->

    $('#about').click ->
        host().showAbout ->

    $('#contexts').on 'change', '.check input', ->
        ctxId = $(this).data 'id'
        ctx = contexts[ctxId]
        if ctx?
            ctx.enabled = $(this).prop 'checked'
            host().setContextEnabled ctx.id, ctx.enabled
    .on 'dblclick', '.check input', ->
        ctxId = $(this).data 'id'
        ctx = contexts[ctxId]
        if ctx?
            $('#contexts .check input').not(this).prop 'checked', false
            for id, c of contexts
                c.enabled = false
            ctx.enabled = true
            $(this).prop 'checked', true
            host().setContextEnabledOnly ctx.id

    $('#contexts').on 'click', '.text_s', ->
        $this = $(this)
        ofs = $this.offset()
        ctxId = Number $this.data 'id'
        host().showLog ctxId

    $('#newContexts').change ->
        host().setNewContextsBehavior $('#newContexts').val()

contexts = {}

formatProcessName = (name) ->
    parts = name.split "\\"
    if parts.length > 2
        "...\\" + parts[-2..].join "\\"
    else
        name

setDefaultProcess = window.setDefaultProcess = (pid, name) ->
    $opDefault = $ '#process_default'
    $opDefault.val(pid).data('exe', name).html(formatProcessName name)
    $('#process').val(pid)
    return

selectWindowEnd = window.selectWindowEnd = ->
    isSelectWindow = false
    $('#connect_status').empty()

connectError = window.connectError = (errMsg) ->
    $('#connect').prop 'disabled', false
    $('#connect_status').html errMsg
    return

clearContexts = ->
    contexts = {}
    $('#contexts').empty()
    return

connectSuccess = window.connectSuccess = (newContextsValue) ->
    $('#startup').hide()
    clearContexts()
    $('#newContexts').val(newContextsValue)
    $('body').removeClass('startup').addClass('working')
    $('#working').show()
    return

disconnect = window.disconnect = ->
    $('#working').hide()
    connectError('Disconnected.');
    $('body').removeClass('working').addClass('startup')
    $('#startup').show()
    return

log = window.log = (s) ->
    $('<div>').text(s).appendTo $('#log')
    return

logParse = window.logParse = (s) ->
    $('<div style="margin-bottom:1em"></div>').text(s).prependTo $('#parse')
    return

longToHex = (addr) ->
    res = for i in [1..8]
        x = addr % 16
        addr = addr / 16 | 0
        "0123456789ABCDEF".charAt x
    res.reverse().join('')

formatAddr = (addr) ->
    addrText = longToHex addr
    addrText.substr(0, 4) + ':' + addrText.substr(4)

newContext = window.newContext = (id, name, addr, sub, enabled) ->
    # log('new ctx: ' + name)
    ctx = {id: id, name: name, addr: addr, sub: sub, enabled: enabled}
    nameStr = name
    if sub
        nameStr += " (" + sub + ")"
    ctx.tr = $ """<tr>
        <td class="check"><input type=checkbox title="Double click to select only this context" data-id="#{id}" #{if enabled then 'checked' else ''} /></td>
        <td class="addr">[#{formatAddr(addr, sub)}]</td>
        <td class="name"><span class="name_s">#{nameStr}</span></td>
        <td class="text"><div class="text_s" data-id="#{id}"></div></td>
    </tr>"""
    contexts[id] = ctx
    $('#contexts').append ctx.tr
    return

newSentence = window.newSentence = (id, text) ->
    # log('text ' + id)
    ctx = contexts[id]
    if not ctx?
        ctxData = host().getContext id
        if ctxData?
            {id, name, addr, sub, enabled} = ctxData
            newContext id, name, addr, sub, enabled
            ctx = contexts[id]
    if ctx?
        ctx.tr.find('.text_s').html _.escape text
        if ctx.enabled
            $('#contexts').prepend ctx.tr
    return

disableContexts = window.disableContexts = (idsJson) ->
    ids = JSON.parse idsJson
    for id in ids
        ctx = contexts[id]
        if ctx?
            ctx.enabled = false
            ctx.tr.find('.check input').prop('checked', false)
    return