evalAsJson = (json) ->
    fn = new Function "return " + json
    fn()

get = (options, callback, func) ->
    http.request options, (res, err) ->
        if err
            callback err
        else
            try
                result = func res
            catch ex
                result = ex.toString()
            callback result

html2text = (html) ->
    $tmp = $ "<span>" + html + "</span>"
    $.trim $tmp.text()

registerTranslators
    "ATLAS": (src, callback) ->
        host().translateAtlas src, callback

    "Custom": (src, callback, ex) ->
        host().translateCustom ex.rawText, callback

    "Google": (src, callback) ->
        src = encodeURIComponent src
        url = "http://translate.google.com/translate_a/t?client=t&text=#{src}&sl=ja&tl=en"
        get url, callback, (res) ->
            res = evalAsJson res
            res[0][0][0]

    "Babylon": (src, callback) ->
        src = encodeURIComponent src.replace(/「/g, '"').replace(/」/g, '"')
        url = "http://translation.babylon.com/translate/babylon.php?v=1.0&q=#{src}&langpair=8%7C0&callback=ret"
        get url, callback, (jsonp) ->
            (new Function 'ret', 'return ' + jsonp) (_, a) ->
                html2text a.translatedText

    "SDL": (src, callback) ->
        src = encodeURIComponent src
        url = "http://tets9.freetranslation.com/?sequence=core&charset=UTF-8&language=Japanese%2FEnglish&srctext=#{src}"
        get { url: url, method: 'post' }, callback, (res) -> res

    "Excite": (src, callback) ->
        src = encodeURIComponent src
        url = "http://www.excite.co.jp/world/english/?wb_lp=JAEN&before=#{src}"
        get { url: url, method: 'post' }, callback, (res) -> 
            res = /\<textarea id="after".*?\>([^]*?)\<\/textarea\>/.exec res
            html2text res[1]

    "Honyaku": (src, callback) ->
        src = encodeURIComponent src
        url = "http://honyaku.yahoo.co.jp/transtext?both=TH&eid=CR-JE&text=#{src}"
        get { url: url, method: 'post' }, callback, (res) ->
            res = /id="trn_textText".*?\>([^]*?)\<\/textarea\>/.exec res
            res[1]

    "Microsoft": (src, callback) ->
        src = encodeURIComponent JSON.stringify [src]
        url = "http://api.microsofttranslator.com/v2/ajax.svc/TranslateArray?from=%22ja%22&to=%22en%22&appId=%22F84955C82256C25518548EE0C161B0BF87681F2F%22&texts=#{src}"
        get url, callback, (res) ->
            res = evalAsJson res
            res[0].TranslatedText

    "SysTran": (src, callback) ->
        # src = encodeURIComponent src
        url = "http://www.systranet.com/sai?lp=ja_en&service=translate"
        get { url: url, method: 'post', query: src }, callback, (res) ->
            $.trim res.replace("body=", "")