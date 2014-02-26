MAX_LOG = 20
$history = null
$content = null
$current = null
$trans = null
$font = null
separateWords = false
separateSpeaker = false

roundTo1_100 = (n) ->
    Math.round(n * 100) / 100

$ ->
    options = host().getOptions()

    setTransparentMode(options.transparentMode)

    captionLastClick = 0
    $('#caption').mousedown (ev) ->
        host().dragForm()
        now = new Date().getTime()
        if now - captionLastClick < 300
            host().dblClickCaption()
        else
            captionLastClick = now

    $('.caption_btn').mousedown (ev) ->
        ev.stopPropagation()
    
    $('#minimize').click ->
        host().formMinimize()

    $('#close').click ->
        host().formClose()

    $('#transparent').click ->
        $('html').toggleClass('transparent')
        host().setTransparentMode $('html').hasClass('transparent')

    makeResizer = (ctl, dx, dy) ->
        $('#' + ctl).mousedown (ev) ->
            ev.preventDefault()
            host().resizeForm(dx, dy)
    
    makeResizer('sideTop', 0, -1)
    makeResizer('sideBottom', 0, 1)
    makeResizer('sideLeft', -1, 0)
    makeResizer('sideRight', 1, 0)
    makeResizer('sideTopLeft', -1, -1)
    makeResizer('sideBottomLeft', -1, 1)
    makeResizer('sideTopRight', 1, -1)
    makeResizer('sideBottomRight', 1, 1)

    $('#sideRightScroll, #sideRight').mouseenter ->
        if $history.css('display') == 'none'
            $history.show()
            $content.scrollTop 99999
            $(document).on 'mousemove.scroll', (ev) ->
                if (ev.pageX < $(window).width() - 20)
                    if $content.scrollTop() >= $history.height() - 10
                        $history.hide()
                    $(document).off('mousemove.scroll')
        return

    hiding = false
    hidingTimer = null

    $(document).on('mouseenter', '.basetext', (ev) ->
        if hiding
            hiding = false
            clearTimeout hidingTimer
        $this = $(this).parents('.parsed')
        ofs = $this.offset()
        host().showHint($this.parents('.entry').data('id'), $this.data('num'),
            Math.round(ofs.left), Math.round(ofs.top), $this.height(), 
            $(window).width(), $(window).height(), ->)
    ).on('mouseleave', '.parsed', (ev) ->
        if not hiding
            hiding = true
            hideFunc = ->
                if hiding
                    hiding = false
                    host().hideHint(->)
            hidingTimer = setTimeout hideFunc, 200
    ).on('mouseup', (ev) ->
        if ev.which == 3
            ev.preventDefault()
            host().showContextMenu getTextSelection(), true, getSelectedEntryId()
        return
    ).on('mouseup', '.parsed', (ev) ->
        if ev.which == 3
            ev.preventDefault()
            ev.stopPropagation()
            sel = getTextSelection()
            if sel? and sel != ""
                host().showContextMenu sel, true, getSelectedEntryId()
            else
                host().showContextMenu $(this).data('text'), false, $(this).parents('.entry').data('id')
        return
    )

    $content = $('#content')
    $history = $('#history')
    $current = $('#current')

    document_attachEvent 'onmousewheel', (e) ->
        ev = $.Event e
        if host().onWheel(-(e.wheelDelta / 120))
            ev.preventDefault()
        else
            if e.wheelDelta > 0 and $history.css('display') == 'none'
                $history.show()
                $content.scrollTop 99999
        return

    $(document).keydown (ev) ->
        if ev.keyCode == 32
            ev.preventDefault();

    $trans = $('#trans_slider').slider
        orientation: 'vertical'
        min: 0
        max: 100
        step: 0.01
        value: options.transparencyLevel
        slide: (ev, ui) ->
            setTransparencyLevel ui.value
            host().setTransparencyLevel ui.value
            return
    makePopupSlider $('#trans_slider'), $('#transparent')
    setTransparencyLevel options.transparencyLevel

    fromLg = (v) ->
        v = (-v) / 100
        roundTo1_100 100 * Math.pow 4, v

    toLg = (v) ->
        v = v / 100
        -(100 * (Math.log v) / (Math.log 4))
    
    $font = $('#font_slider').slider
        orientation: 'vertical'
        min: -100
        max: 100
        value: toLg options.fontSize
        slide: (ev, ui) ->
            v = fromLg ui.value
            setFontSize v
            host().setFontSize v
            return
    .dblclick ->
        v = prompt "Enter font size: ", fromLg $font.slider('value')
        if v and not isNaN Number v
            v = Number v
            $font.slider 'value', toLg v
            setFontSize v
            host().setFontSize v
            return
    makePopupSlider $('#font_slider'), $('#font_size')
    setFontSize options.fontSize

    $('#font_size').click ->
        setFontSize 100
        host().setFontSize 100
        $('#font_slider').slider('value', toLg 100)
    
lastEntryId = -1
lastParseResult = null
$currentEntry = null

newTranslationResult = window.newTranslationResult = (id, translationResult) ->
    updateTranslationResult id, renderOldTranslationResult translationResult

updateTranslationResult = (id, translationResultHtml) ->
    if id < lastEntryId
        entry = $history.find(""".entry[data-id="#{id}"]""")
    else
        if id > lastEntryId
            onNewEntry id
        entry = $currentEntry
    if entry.length
        $('#translation', entry).html translationResultHtml
    return
    

newParseResult = window.newParseResult = (id, parseResult) ->
    if id < lastEntryId
        entry = $history.find(""".entry[data-id="#{id}"]""")
    else
        if id > lastEntryId
            onNewEntry id
        entry = $currentEntry
    if entry.length
        $('#parse', entry).html renderParseResult parseResult
    return

setTransparentMode = window.setTransparentMode = (isEnabled) ->
    aero = host().getAero()
    if aero
        $('html').removeClass('no_aero').addClass('aero')
    else
        $('html').removeClass('aero').addClass('no_aero')
    if isEnabled
        $('html').addClass('transparent')
    else
        $('html').removeClass('transparent')
    return

onNewEntry = (id) ->
    lastEntryId = id
    if $history.css('display') != 'none'
        if $content.scrollTop() >= $history.height() - 10
            $history.hide()
    moveToHistory $currentEntry if $currentEntry?
    $currentEntry = createNewEntry id

getTextSelection = window.getTextSelection = ->
    if document.selection?
        document.selection.createRange().text
    else
        window.getSelection().toString()

getSelectedEntryId = window.getSelectedEntryId = ->
    if window.getSelection?
        sel = window.getSelection()
        node1 = $ sel.anchorNode
        node2 = $ sel.focusNode
        res = node1.parents('.entry').data('id') ? node2.parents('.entry').data('id')
        if res?
            +res
        else
            0
    else
        0

updateReading = window.updateReading = (text, reading) ->
    if $currentEntry?
        rts = $currentEntry.find('rt')
        for _rt in rts
            rt = $ _rt
            if rt.data('text') == text
                rt.html "&#8203;#{_.escape reading}&#8203;"
    return

setSeparateWords = window.setSeparateWords = (b) ->
    separateWords = b
    return

setSeparateSpeaker = window.setSeparateSpeaker = (b) ->
    separateSpeaker = b
    return

moveToHistory = (entry) ->
    $history.append entry
    historyEntries = $history.find('>.entry')
    if historyEntries.length > MAX_LOG
        historyEntries.first().remove()

createNewEntry = (id) ->
    $res = $ ("""<div class="entry" data-id="#{id}">""" +
        """<div class="parse_frame"><div class="font_zoom"><div id="parse"></div></div></div>""" +
        """<div class="translation_frame"><div class="font_zoom"><div id="translation"></div></div></div>""" +
        """</div>""")
    $current.append $res
    $res
    
renderParseResult = (p) ->
    parseResult = JSON.parse p
    lastParseResult = parseResult
    res = $ "<span>"
    colorNum = 0
    i = 0
    format = (s) ->
        if not s?
            return ""
        q = for j in [0 ... s.length]
            "<span>" + (_.escape s.charAt j) + "</span>"
        q.join('')
    okuri = parseResult.okuri
    breakIndex = -1
    if separateSpeaker
        lastPart = parseResult.parts[parseResult.parts.length - 1]
        if not _.isArray(lastPart)
            lastChar = lastPart.charAt lastPart.length - 1
            if lastChar in ["』", "」"]
                if lastChar == "』" then firstChar = "『" else firstChar = "「"
                for j in [0 ... parseResult.parts.length]
                    part = parseResult.parts[j]
                    if not _.isArray(part)
                        firstCharIdx = part.indexOf(firstChar)
                        if firstCharIdx != -1 and not (j == 0 and firstCharIdx == 0) 
                            breakIndex = j
                            break
    for part in parseResult.parts
        if _.isArray part
            [text, stem, reading, isName] = part
            parsedClassSuffix = if isName then "_name" else colorNum
            if not reading? or reading == ""
                $block = $ """<span class="noruby"><span data-num="#{i}" class="text parsed parsed#{parsedClassSuffix}"><span class="basetext">#{format text}</span></span></span>"""
            else
                readingFormatted = "&#8203;#{_.escape reading}&#8203;"
                if okuri == "NORMAL"
                    if text.substr(0, stem.length) != stem
                        #arroor?
                        stem = text
                    inf = text.substr(stem.length)
                    $block = $ """<span class="ruby"><span data-num="#{i}" class="text parsed parsed#{parsedClassSuffix}"><ruby class="normal"><span class="basetext">#{format stem}</span><rt>#{readingFormatted}</rt></ruby><span class="basetext">#{format inf}</span></span></span>"""
                    $block.find('rt').data('text', stem)
                else
                    $block = $ """<span class="ruby"><span data-num="#{i}" class="text parsed parsed#{parsedClassSuffix}"><ruby class="special"><span class="basetext">#{format text}</span><rt>#{readingFormatted}</rt></ruby></span></span>"""
                    $block.find('rt').data('text', text)
            $block.find('.parsed').data('text', text)
            res.append $block
            colorNum = 1 - colorNum
        else
            unparsed = format part
            if i == breakIndex
                unparsed = unparsed.replace(firstChar, '<br />' + firstChar)
            res.append $ """<span data-num="#{i}" class="text unparsed">#{unparsed}</span>"""
        if separateWords
            res.append "<span>&#8203; &#8203;</span>"
        i += 1
    res

renderOldTranslationResult = (tr) ->
    tr = JSON.parse tr
    $res = $('<span>')
    if not tr.isAtlas
        $res.addClass 'no_atlas'
    else
        $res.addClass 'atlas'
    $res.html _.escape(tr.text).replace(/\n/g, '<br>')
    $res

renderSimpleTranslationResult = (text) ->
    $res = $('<span>')
    $res.html _.escape(text).replace(/\n/g, '<br>')
    $res

renderMultiTranslationResult = (translators) ->
    $res = $('<table class="multiTranslation">')
    for t in translators
        $res.append """
            <tr>
                <td class="translator">#{t}</td>
                <td class="result" data-trans="#{t}"></td>
            </tr>"""
    $res

updateMultiTranslationResult = (el, trans, text) ->
    $(""".result[data-trans="#{trans}"]""", el)
        .html _.escape(text).replace(/\n/g, '<br>') 

log = (s) ->
    $('<div>').text(s).appendTo $('#log')
    return

flash = window.flash = (s) ->
    $('#flash').finish().text(s).show().fadeOut(3000)
    return

makePopupSlider = (slider, trig) ->
    isMouseInside = false
    isMouseDown = false

    $doc = $(document)

    slider
    .mouseenter ->
        isMouseInside = true
    .mouseleave ->
        isMouseInside = false
        if not isMouseDown 
            slider.hide()
    .mousedown ->
        isMouseDown = true
        $doc.one 'mouseup', ->
            isMouseDown = false
            if not isMouseInside
                slider.hide()

    trig.mouseenter ->
        slider.show()
    .mouseleave ->
        setTimeout ->
            if not isMouseDown and not isMouseInside
                slider.hide()
        , 100
    return

setTransparencyLevel = (lvl) ->
    $trans.attr 'title', "Transparency level: #{roundTo1_100 lvl}%"

setFontSize = (size) ->
    $font.attr 'title', "Font size: #{roundTo1_100 size}%"
    try
        $('#fontSizeStyle')[0].styleSheet.cssText = ".font_zoom { font-size: #{size}% }"
    catch e
        $('#fontSizeStyle').html ".font_zoom { font-size: #{size}% }"
    return


translators = {}

registerTranslators = window.registerTranslators = (trans) ->
    translators = trans
    host().registerTranslators _.keys trans

translate = window.translate = (id, raw, src, translatorsListJson) ->
    translatorsList = JSON.parse translatorsListJson
    ex = { id: id, rawText: raw }
    if translatorsList.length <= 1
        for t in translatorsList
            translators[t] src, (res) -> 
                updateTranslationResult id, renderSimpleTranslationResult res
            , ex
    else
        container = renderMultiTranslationResult translatorsList
        updateTranslationResult id, container
        for t in translatorsList 
            do (t) =>
                translators[t] src, (res) ->
                    updateMultiTranslationResult container, t, res
                , ex
    return

http = window.http =
    request: (options, cb) ->
        if typeof options == "string"
            options = url : options
        _.defaults options,
            useShiftJis: false
            method: "get"
        if options.method.toLowerCase() != "get" and not options.query?
            [options.url, options.query] = options.url.split '?'
        host().httpRequest options.url, options.useShiftJis, options.method, options.query, (res) ->
            cb(res.res, res.error)