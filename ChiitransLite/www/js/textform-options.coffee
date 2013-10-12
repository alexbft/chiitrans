isDirty = false

$ ->
    $('input[type=radio], input[type=checkbox]').change ->
        setDirty true

    $('#ok').click ->
        saveOptions()
        host().close()

    $('#apply').click ->
        saveOptions()

    $('#cancel').click ->
        host().close()

    $('#theme').change ->
        setDirty true

    $('#reset').click ->
        host().resetParsePreferences()
    
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
    
setRadioValue = (name, value) ->
    $("""input[type=radio][name="#{name}"][value="#{value}"]""").prop('checked', true)

getRadioValue = (name) ->
    $("""input[type=radio][name="#{name}"]:checked""").val()

resetOptionsInt = (op) ->
    setRadioValue "display", op.display
    setRadioValue "okuri", op.okuri
    $theme = $('#theme')
    $theme.html("""<option value="">Default</option>""")
    for theme in op.themes
        $theme.append("""<option value="#{_.escape theme}">#{_.escape theme}</option>""")
    $theme.val(op.theme ? "")
    $('#separateWords').prop 'checked', op.separateWords
    $('#reset').blur();
    $('#ok').focus();
    setDirty false

saveOptions = ->
    if isDirty
        op =
            display: getRadioValue "display"
            okuri: getRadioValue "okuri"
            theme: $('#theme').val()
            separateWords: $('#separateWords').prop('checked')
        host().saveOptions op
        setDirty false