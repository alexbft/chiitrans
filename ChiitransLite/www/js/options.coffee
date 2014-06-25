isDirty = false
isClipboardTranslation = false

$ ->
    $('input[type=radio], input[type=checkbox], input[type=text], select').change ->
        setDirty true

    $('#ok').click ->
        saveOptions()
        host().close()

    $('#apply').click ->
        saveOptions()

    $('#cancel').click ->
        host().close()

    $('#clipboard').click ->
        setClipboardTranslation !isClipboardTranslation
        setDirty true

    $('#showHooks').click ->
        host().showHookForm()
        
    $('#showNames').click ->
        host().showNamesForm ->

    $('#showPoFiles').click ->
        host().showPoFiles ->
        
    $('#reset').click ->
        host().resetParsePreferences()

    $('#showExtraTranslators').click ->
        host().showExtraTranslators ->

    resetOptionsInt host().getOptions()
    $('.menu>li').click ->
        selectTab $ this
    selectTab $('.menu li:first')

resetOptions = window.resetOptions = (opJson) ->
    op = JSON.parse opJson
    $ ->
        resetOptionsInt op
    return

setDirty = (_isDirty) ->
    isDirty = _isDirty
    $('#apply').prop('disabled', !isDirty)
    return

setClipboardTranslation = (isEnabled) ->
    isClipboardTranslation = isEnabled
    if isEnabled
        $('#clipboard').addClass('pressed').attr('title', 'Disable Capture text from Clipboard')
    else
        $('#clipboard').removeClass('pressed').attr('title', 'Enable Capture text from Clipboard')
    return

setRadioValue = (name, value) ->
    $("""input[type=radio][name="#{name}"][value="#{value}"]""").prop('checked', true)

getRadioValue = (name) ->
    $("""input[type=radio][name="#{name}"]:checked""").val()
    
resetOptionsInt = (op) ->
    setClipboardTranslation op.clipboard
    $('#sentenceDelay').val op.sentenceDelay
    $('.btn').addClass('enabled')
    $('#showHooks').prop('disabled', !op.enableHooks)
    if !op.enableHooks
        $('#showHooks').removeClass('enabled')
    $('#sentenceDelay').prop('disabled', !op.enableSentenceDelay)
    if !op.enableSentenceDelay
        $('#sentenceDelay').removeClass('enabled')
    setRadioValue "display", op.display
    setRadioValue "okuri", op.okuri
    setRadioValue "nameDict", op.nameDict
    $theme = $('#theme')
    $theme.html("""<option value="">Default</option>""")
    for theme in op.themes
        $theme.append """<option value="#{_.escape theme}">#{_.escape theme}</option>"""
    $theme.val(op.theme ? "")
    $atlasEnv = $('#atlasEnv')
    $atlasEnv.empty()
    for env in op.atlasEnvList
        $atlasEnv.append """<option value="#{_.escape env}">#{_.escape env}</option>"""
    $atlasEnv.val op.atlasEnv
    $('#separateWords').prop 'checked', op.separateWords
    $('#separateSpeaker').prop 'checked', op.separateSpeaker
    $('#stayOnTop').prop 'checked', op.stayOnTop
    $('#ok').focus()
    setDirty false

saveOptions = ->
    if isDirty
        op =
            clipboard: isClipboardTranslation
            sentenceDelay: parseInt($('#sentenceDelay').val(), 10)
            display: getRadioValue "display"
            okuri: getRadioValue "okuri"
            nameDict: getRadioValue "nameDict"
            theme: $('#theme').val()
            separateWords: $('#separateWords').prop('checked')
            separateSpeaker: $('#separateSpeaker').prop('checked')
            atlasEnv: $('#atlasEnv').val()
            stayOnTop: $('#stayOnTop').prop('checked')
        host().saveOptions op
        setDirty false

selectTab = (li) ->
    $('.menu>li').removeClass 'active'
    $('.tab').removeClass 'active'
    li.addClass 'active'
    tabId = li.data 'target'
    $("##{tabId}").addClass 'active'
    return