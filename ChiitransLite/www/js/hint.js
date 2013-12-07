(function() {
  var $hint, $infType, $kana, $kanji, $nameType, $page, $pageNum, $pageNumFrame, $sense, $tense, data, defs, makeDictKey, makeMisc, makeMiscSingle, makeReading, makeSense, nameDefs, pageNext, pageNum, pagePrev, setDefinitions, setNameDefinitions, show, showPage, showSense;

  data = null;

  pageNum = 0;

  defs = {};

  nameDefs = {};

  $page = $pageNum = $pageNumFrame = $kanji = $kana = $sense = $nameType = $infType = $tense = $hint = null;

  $(function() {
    $(document).mousedown(function(ev) {
      if (ev.which === 3) {
        return host().hideForm();
      }
    }).mouseleave(function() {
      return host().hideForm();
    });
    document_attachEvent('onmousewheel', function(e) {
      host().onWheel(-(e.wheelDelta / 120));
    });
    $('#prevPage').click(function() {
      pagePrev();
    });
    $('#nextPage').click(function() {
      pageNext();
    });
    $page = $('#page');
    $pageNum = $('#pageNum');
    $pageNumFrame = $('#pageNumFrame');
    $kanji = $('#kanji');
    $kana = $('#kana');
    $sense = $('#sense');
    $nameType = $('#nameType');
    $infType = $('#infType');
    $tense = $('#tense');
    $hint = $('#hint');
    return $page.on('click', '.reading', function() {
      host().setReading(data.stem, $(this).data('text'));
    });
  });

  show = window.show = function(json, _pageNum, isTransparent) {
    var found, i, _i, _ref;
    data = JSON.parse(json);
    found = false;
    if (_pageNum !== -1) {
      for (i = _i = 0, _ref = data.word.length; 0 <= _ref ? _i < _ref : _i > _ref; i = 0 <= _ref ? ++_i : --_i) {
        if (data.word[i].pageNum === _pageNum) {
          pageNum = i;
          found = true;
          break;
        }
      }
    }
    if (!found) {
      pageNum = 0;
    }
    if (isTransparent) {
      $('html').addClass('transparent');
    } else {
      $('html').removeClass('transparent');
    }
    return showPage(data, pageNum);
  };

  pagePrev = window.pagePrev = function() {
    if (pageNum > 0) {
      pageNum -= 1;
      showPage(data, pageNum);
      host().setSelectedPage(data.stem, data.word[pageNum].pageNum);
    }
    return (data != null ? data.word.length : void 0) >= 2;
  };

  pageNext = window.pageNext = function() {
    if (pageNum < data.word.length - 1) {
      pageNum += 1;
      showPage(data, pageNum);
      host().setSelectedPage(data.stem, data.word[pageNum].pageNum);
    }
    return (data != null ? data.word.length : void 0) >= 2;
  };

  setDefinitions = window.setDefinitions = function(data) {
    defs = JSON.parse(data);
  };

  setNameDefinitions = window.setNameDefinitions = function(data) {
    nameDefs = JSON.parse(data);
  };

  showPage = function(data, pageNum) {
    var infType, kana, kanji, page, s, sense, showKana, _i, _len, _ref;
    if (data.word.length > 1) {
      $pageNum.html((pageNum + 1) + "/" + data.word.length);
      $pageNumFrame.css({
        visibility: 'visible'
      });
    } else {
      $pageNumFrame.css({
        visibility: 'hidden'
      });
    }
    page = data.word[pageNum];
    if (page != null) {
      kanji = _.map(page.kanji, makeDictKey);
      $kanji.html(kanji.join(', '));
      if (kanji.length > 0 && page.kana.length >= 2) {
        kana = _.map(page.kana, makeReading);
      } else {
        kana = _.map(page.kana, makeDictKey);
      }
      if (kanji.length === 0) {
        showKana = $kanji;
        $kana.empty();
      } else {
        showKana = $kana;
      }
      showKana.html(kana.join(', '));
      sense = $('<ul>');
      _ref = page.sense.slice(0, 5);
      for (_i = 0, _len = _ref.length; _i < _len; _i++) {
        s = _ref[_i];
        sense.append(makeSense(s));
      }
      $sense.html(sense);
      if (page.sense.length > 5) {
        $("<div style=\"text-align:center\"><a onclick=\"showSense(); return false;\" href=\"#\">(" + (page.sense.length - 5) + " more...)</a></div>").appendTo($sense);
      }
      if (page.nameType != null) {
        $nameType.html(makeMisc([page.nameType]));
      } else {
        $nameType.empty();
      }
      infType = null;
      if (data.inf.isFormal && data.inf.isNegative) {
        infType = "Formal, negative";
      } else if (data.inf.isFormal) {
        infType = "Formal";
      } else if (data.inf.isNegative) {
        infType = "Negative";
      }
      if (infType != null) {
        $infType.html(infType);
      } else {
        $infType.empty();
      }
      if ((page.POS != null) && page.POS.length) {
        $tense.html(makeMisc(page.POS));
        $tense.append("<span> </span>");
      } else {
        $tense.empty();
      }
      if (data.inf.tense != null) {
        $tense.append(data.inf.tense);
      }
      $page.show();
    } else {
      $page.hide();
    }
    return host().setHeight($hint.height() + 5);
  };

  showSense = window.showSense = function() {
    var page, s, sense, _i, _len, _ref;
    page = data.word[pageNum];
    sense = $('<ul>');
    _ref = page.sense;
    for (_i = 0, _len = _ref.length; _i < _len; _i++) {
      s = _ref[_i];
      sense.append(makeSense(s));
    }
    $sense.html(sense);
    host().setHeight($hint.height() + 5);
  };

  makeDictKey = function(d) {
    var misc;
    misc = d.misc != null ? makeMisc(d.misc) : "";
    return "<span class=\"dict_key\"><span class=\"key\">" + (_.escape(d.text)) + "</span>" + misc + "</span>";
  };

  makeReading = function(d) {
    var misc, txt;
    misc = d.misc != null ? makeMisc(d.misc) : "";
    txt = _.escape(d.text);
    return "<span class=\"dict_key\"><span class=\"key reading\" data-text=\"" + txt + "\" title=\"Select this reading\">" + txt + "</span>" + misc + "</span>";
  };

  makeMisc = function(misc) {
    return "<span class=\"misc_set\">(" + (_.map(misc, makeMiscSingle).join(', ')) + ")</span>";
  };

  makeMiscSingle = function(misc) {
    var src, title;
    src = data.isName ? nameDefs : defs;
    if (src[misc] != null) {
      title = " title=\"" + (_.escape(src[misc])) + "\"";
    } else {
      title = "";
    }
    return "<span class=\"misc\"" + title + ">" + misc + "</span>";
  };

  makeSense = function(sense) {
    var res;
    res = $('<li class="sense"></li>');
    res.append("<span class=\"glossary\">" + (_.map(sense.glossary, _.escape).join('; ')) + "</span>");
    if (sense.misc != null) {
      res.append(" " + (makeMisc(sense.misc)));
    }
    return res;
  };

}).call(this);
