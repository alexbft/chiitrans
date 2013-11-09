data = null
pageNum = 0
defs = {}
nameDefs = {}

$page = $pageNum = $pageNumFrame = $kanji = $kana = $sense = $nameType = $infType = $tense = $hint = null

$ ->
    $(document)
    .mousedown (ev) ->
        if ev.which == 3
            host().hideForm()
    .mouseleave ->
        host().hideForm()

    document_attachEvent 'onmousewheel', (e) ->
        host().onWheel(-(e.wheelDelta / 120))
        return

    $('#prevPage').click ->
        pagePrev()
        return
    
    $('#nextPage').click ->
        pageNext()
        return

    $page = $('#page')
    $pageNum = $('#pageNum')
    $pageNumFrame = $('#pageNumFrame')
    $kanji = $('#kanji')
    $kana = $('#kana')
    $sense = $('#sense')
    $nameType = $('#nameType')
    $infType = $('#infType')
    $tense = $('#tense')
    $hint = $('#hint')

    $page.on 'click', '.reading', ->
        host().setReading data.stem, $(this).data('text')
        return

show = window.show = (json, _pageNum, isTransparent) ->
    data = JSON.parse json
    found = false
    if _pageNum != -1
        for i in [0 ... data.word.length]
            if data.word[i].pageNum == _pageNum
                pageNum = i
                found = true
                break
    if not found
        pageNum = 0
    if isTransparent
        $('html').addClass('transparent')
    else
        $('html').removeClass('transparent')
    showPage(data, pageNum)

pagePrev = window.pagePrev = ->
    if pageNum > 0
        pageNum -= 1
        showPage(data, pageNum)
        host().setSelectedPage(data.stem, data.word[pageNum].pageNum)
    return data?.word.length >= 2

pageNext = window.pageNext = ->
    if pageNum < data.word.length - 1
        pageNum += 1
        showPage(data, pageNum)
        host().setSelectedPage(data.stem, data.word[pageNum].pageNum)
    return data?.word.length >= 2

setDefinitions = window.setDefinitions = (data) ->
    defs = JSON.parse data
    return

setNameDefinitions = window.setNameDefinitions = (data) ->
    nameDefs = JSON.parse data
    return

showPage = (data, pageNum) ->
    $page.hide()
    if data.word.length > 1
        $pageNum.html (pageNum + 1) + "/" + data.word.length
        $pageNumFrame.css visibility: 'visible'
    else
        $pageNumFrame.css visibility: 'hidden'
    page = data.word[pageNum]
    if page?
        kanji = _.map page.kanji, makeDictKey
        $kanji.html kanji.join(', ')
        if kanji.length > 0 and page.kana.length >= 2 #enable changing reading only if readings >= 2
            kana = _.map page.kana, makeReading
        else
            kana = _.map page.kana, makeDictKey
        if kanji.length == 0
            showKana = $kanji
            $kana.empty()
        else
            showKana = $kana
        showKana.html kana.join(', ')
        sense = $('<ul>')
        for s in page.sense[...5]
            sense.append makeSense s
        $sense.html sense
        if page.sense.length > 5
            $("""<div style="text-align:center"><a onclick="showSense(); return false;" href="#">(#{page.sense.length - 5} more...)</a></div>""").appendTo($sense)
        if page.nameType?
            $nameType.html makeMisc [page.nameType]
        else
            $nameType.empty()
        infType = null
        if data.inf.isFormal and data.inf.isNegative
            infType = "Formal, negative"
        else if data.inf.isFormal
            infType = "Formal"
        else if data.inf.isNegative
            infType = "Negative"
        if infType?
            $infType.html infType
        else
            $infType.empty()
        if data.inf.tense?
            $tense.html data.inf.tense
        else
            $tense.empty()
        $page.show()
    host().setHeight($hint.height() + 5)

showSense = window.showSense = ->
    page = data.word[pageNum]
    sense = $('<ul>')
    for s in page.sense
        sense.append makeSense s
    $sense.html sense
    host().setHeight($hint.height() + 5)
    return

makeDictKey = (d) ->
    misc = if d.misc? then makeMisc(d.misc) else ""
    """<span class="dict_key"><span class="key">#{_.escape d.text}</span>#{misc}</span>"""

makeReading = (d) ->
    misc = if d.misc? then makeMisc(d.misc) else ""
    txt = _.escape d.text
    """<span class="dict_key"><span class="key reading" data-text="#{txt}" title="Select this reading">#{txt}</span>#{misc}</span>"""

makeMisc = (misc) ->
    """<span class="misc_set">(#{_.map(misc, makeMiscSingle).join(', ')})</span>"""

makeMiscSingle = (misc) ->
    src = if data.isName then nameDefs else defs
    if src[misc]?
        title = " title=\"#{_.escape src[misc]}\""
    else
        title = ""
    """<span class="misc"#{title}>#{misc}</span>"""

makeSense = (sense) ->
    res = $('<li class="sense"></li>')
    res.append """<span class="glossary">#{_.map(sense.glossary, _.escape).join('; ')}</span>"""
    if sense.misc?
        res.append " " + (makeMisc sense.misc)
    res
    