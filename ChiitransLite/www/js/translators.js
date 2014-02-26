(function() {
  var evalAsJson, get, html2text;

  evalAsJson = function(json) {
    var fn;
    fn = new Function("return " + json);
    return fn();
  };

  get = function(options, callback, func) {
    return http.request(options, function(res, err) {
      var result;
      if (err) {
        return callback(err);
      } else {
        try {
          result = func(res);
        } catch (ex) {
          result = ex.toString();
        }
        return callback(result);
      }
    });
  };

  html2text = function(html) {
    var $tmp;
    $tmp = $("<span>" + html + "</span>");
    return $.trim($tmp.text());
  };

  registerTranslators({
    "ATLAS": function(src, callback) {
      return host().translateAtlas(src, callback);
    },
    "Custom": function(src, callback, ex) {
      return host().translateCustom(ex.rawText, callback);
    },
    "Google": function(src, callback) {
      var url;
      src = encodeURIComponent(src);
      url = "http://translate.google.com/translate_a/t?client=t&text=" + src + "&sl=ja&tl=en";
      return get(url, callback, function(res) {
        res = evalAsJson(res);
        return res[0][0][0];
      });
    },
    "Babylon": function(src, callback) {
      var url;
      src = encodeURIComponent(src.replace(/「/g, '"').replace(/」/g, '"'));
      url = "http://translation.babylon.com/translate/babylon.php?v=1.0&q=" + src + "&langpair=8%7C0&callback=ret";
      return get(url, callback, function(jsonp) {
        return (new Function('ret', 'return ' + jsonp))(function(_, a) {
          return html2text(a.translatedText);
        });
      });
    },
    "SDL": function(src, callback) {
      var url;
      src = encodeURIComponent(src);
      url = "http://tets9.freetranslation.com/?sequence=core&charset=UTF-8&language=Japanese%2FEnglish&srctext=" + src;
      return get({
        url: url,
        method: 'post'
      }, callback, function(res) {
        return res;
      });
    },
    "Excite": function(src, callback) {
      var url;
      src = encodeURIComponent(src);
      url = "http://www.excite.co.jp/world/english/?wb_lp=JAEN&before=" + src;
      return get({
        url: url,
        method: 'post'
      }, callback, function(res) {
        res = /\<textarea id="after".*?\>([^]*?)\<\/textarea\>/.exec(res);
        return html2text(res[1]);
      });
    },
    "Honyaku": function(src, callback) {
      var url;
      src = encodeURIComponent(src);
      url = "http://honyaku.yahoo.co.jp/transtext?both=TH&eid=CR-JE&text=" + src;
      return get({
        url: url,
        method: 'post'
      }, callback, function(res) {
        res = /id="trn_textText".*?\>([^]*?)\<\/textarea\>/.exec(res);
        return res[1];
      });
    },
    "Microsoft": function(src, callback) {
      var url;
      src = encodeURIComponent(JSON.stringify([src]));
      url = "http://api.microsofttranslator.com/v2/ajax.svc/TranslateArray?from=%22ja%22&to=%22en%22&appId=%22F84955C82256C25518548EE0C161B0BF87681F2F%22&texts=" + src;
      return get(url, callback, function(res) {
        res = evalAsJson(res);
        return res[0].TranslatedText;
      });
    },
    "SysTran": function(src, callback) {
      var url;
      url = "http://www.systranet.com/sai?lp=ja_en&service=translate";
      return get({
        url: url,
        method: 'post',
        query: src
      }, callback, function(res) {
        return $.trim(res.replace("body=", ""));
      });
    }
  });

}).call(this);
