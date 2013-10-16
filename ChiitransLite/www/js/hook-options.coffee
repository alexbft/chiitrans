isDirty = false
isClipboardTranslation = false

$ ->
    $('input[type=radio], input[type=checkbox], input[type=text]').change ->
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

    $('#showPoFiles').click ->
        host().showPoFiles ->        

    resetOptionsInt host().getOptions()

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
    $('#ok').focus();
    setDirty false

saveOptions = ->
    if isDirty
        op =
            clipboard: isClipboardTranslation
            sentenceDelay: parseInt($('#sentenceDelay').val(), 10)
        host().saveOptions op
        setDirty false