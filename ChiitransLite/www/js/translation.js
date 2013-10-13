(function() {
  var $content, $current, $currentEntry, $history, MAX_LOG, createNewEntry, flash, getSelectedEntryId, getTextSelection, lastEntryId, lastParseResult, log, makePopupSlider, moveToHistory, newParseResult, newTranslationResult, onNewEntry, renderParseResult, separateSpeaker, separateWords, setFontSize, setSeparateSpeaker, setSeparateWords, setTransparentMode, updateReading;

  MAX_LOG = 20;

  $history = null;

  $content = null;

  $current = null;

  separateWords = false;

  separateSpeaker = false;

  $(function() {
    var captionLastClick, fromLg, hiding, hidingTimer, makeResizer, options, toLg;
    options = host().getOptions();
    setTransparentMode(options.transparentMode);
    setFontSize(options.fontSize);
    captionLastClick = 0;
    $('#caption').mousedown(function(ev) {
      var now;
      host().dragForm();
      now = new Date().getTime();
      if (now - captionLastClick < 300) {
        return host().dblClickCaption();
      } else {
        return captionLastClick = now;
      }
    });
    $('.caption_btn').mousedown(function(ev) {
      return ev.stopPropagation();
    });
    $('#minimize').click(function() {
      return host().formMinimize();
    });
    $('#close').click(function() {
      return host().formClose();
    });
    $('#transparent').click(function() {
      $('html').toggleClass('transparent');
      return host().setTransparentMode($('html').hasClass('transparent'));
    });
    $('#options').click(function() {
      return host().showOptionsForm();
    });
    makeResizer = function(ctl, dx, dy) {
      return $('#' + ctl).mousedown(function(ev) {
        ev.preventDefault();
        return host().resizeForm(dx, dy);
      });
    };
    makeResizer('sideTop', 0, -1);
    makeResizer('sideBottom', 0, 1);
    makeResizer('sideLeft', -1, 0);
    makeResizer('sideRight', 1, 0);
    makeResizer('sideTopLeft', -1, -1);
    makeResizer('sideBottomLeft', -1, 1);
    makeResizer('sideTopRight', 1, -1);
    makeResizer('sideBottomRight', 1, 1);
    hiding = false;
    hidingTimer = null;
    $(document).on('mouseenter', '.basetext', function(ev) {
      var $this, ofs;
      if (hiding) {
        hiding = false;
        clearTimeout(hidingTimer);
      }
      $this = $(this).parents('.parsed');
      ofs = $this.offset();
      return host().showHint($this.parents('.entry').data('id'), $this.data('num'), Math.round(ofs.left), Math.round(ofs.top), $this.height(), $(window).width(), $(window).height(), function() {});
    }).on('mouseleave', '.parsed', function(ev) {
      var hideFunc;
      if (!hiding) {
        hiding = true;
        hideFunc = function() {
          if (hiding) {
            hiding = false;
            return host().hideHint(function() {});
          }
        };
        return hidingTimer = setTimeout(hideFunc, 200);
      }
    }).on('mouseup', function(ev) {
      if (ev.which === 3) {
        ev.preventDefault();
        host().showContextMenu(getTextSelection(), true, getSelectedEntryId());
      }
    }).on('mouseup', '.parsed', function(ev) {
      var sel;
      if (ev.which === 3) {
        ev.preventDefault();
        ev.stopPropagation();
        sel = getTextSelection();
        if ((sel != null) && sel !== "") {
          host().showContextMenu(sel, true, getSelectedEntryId());
        } else {
          host().showContextMenu($(this).data('text'), false, $(this).parents('.entry').data('id'));
        }
      }
    });
    $content = $('#content');
    $history = $('#history');
    $current = $('#current');
    document.attachEvent('onmousewheel', function(e) {
      var ev;
      ev = $.Event(e);
      if (host().onWheel(-(e.wheelDelta / 120))) {
        ev.preventDefault();
      } else {
        if (e.wheelDelta > 0 && $history.css('display') === 'none') {
          $history.show();
          $content.scrollTop(99999);
        }
      }
    });
    $(document).keydown(function(ev) {
      if (ev.keyCode === 32) {
        return ev.preventDefault();
      }
    });
    $('#trans_slider').slider({
      orientation: 'vertical',
      min: 0,
      max: 100,
      value: options.transparencyLevel,
      slide: function(ev, ui) {
        host().setTransparencyLevel(ui.value);
      }
    });
    makePopupSlider($('#trans_slider'), $('#transparent'));
    fromLg = function(v) {
      v = (-v) / 100;
      return 100 * Math.pow(4, v);
    };
    toLg = function(v) {
      v = v / 100;
      return -(100 * (Math.log(v)) / (Math.log(4)));
    };
    $('#font_slider').slider({
      orientation: 'vertical',
      min: -100,
      max: 100,
      value: toLg(options.fontSize),
      slide: function(ev, ui) {
        var v;
        v = fromLg(ui.value);
        setFontSize(v);
        host().setFontSize(Math.round(v));
      }
    });
    makePopupSlider($('#font_slider'), $('#font_size'));
    return $('#font_size').click(function() {
      setFontSize(100);
      host().setFontSize(100);
      return $('#font_slider').slider('value', toLg(100));
    });
  });

  lastEntryId = -1;

  lastParseResult = null;

  $currentEntry = null;

  newTranslationResult = window.newTranslationResult = function(id, text) {
    var entry;
    if (id < lastEntryId) {
      entry = $history.find(".entry[data-id=\"" + id + "\"]");
    } else {
      if (id > lastEntryId) {
        onNewEntry(id);
      }
      entry = $currentEntry;
    }
    if (entry.length) {
      $('#translation', entry).html(_.escape(text).replace(/\n/g, '<br>'));
    }
  };

  newParseResult = window.newParseResult = function(id, parseResult) {
    var entry;
    if (id < lastEntryId) {
      entry = $history.find(".entry[data-id=\"" + id + "\"]");
    } else {
      if (id > lastEntryId) {
        onNewEntry(id);
      }
      entry = $currentEntry;
    }
    if (entry.length) {
      $('#parse', entry).html(renderParseResult(parseResult));
    }
  };

  setTransparentMode = window.setTransparentMode = function(isEnabled) {
    var aero;
    aero = host().getAero();
    if (aero) {
      $('html').removeClass('no_aero').addClass('aero');
    } else {
      $('html').removeClass('aero').addClass('no_aero');
    }
    if (isEnabled) {
      $('html').addClass('transparent');
    } else {
      $('html').removeClass('transparent');
    }
  };

  onNewEntry = function(id) {
    lastEntryId = id;
    if ($history.css('display') !== 'none') {
      if ($content.scrollTop() >= $history.height() - 10) {
        $history.hide();
      }
    }
    if ($currentEntry != null) {
      moveToHistory($currentEntry);
    }
    return $currentEntry = createNewEntry(id);
  };

  getTextSelection = window.getTextSelection = function() {
    return document.selection.createRange().text;
  };

  getSelectedEntryId = window.getSelectedEntryId = function() {
    var node1, node2, res, sel, _ref;
    if (window.getSelection != null) {
      sel = window.getSelection();
      node1 = $(sel.anchorNode);
      node2 = $(sel.focusNode);
      res = (_ref = node1.parents('.entry').data('id')) != null ? _ref : node2.parents('.entry').data('id');
      if (res != null) {
        return +res;
      } else {
        return 0;
      }
    } else {
      return 0;
    }
  };

  updateReading = window.updateReading = function(text, reading) {
    var rt, rts, _i, _len, _rt;
    if ($currentEntry != null) {
      rts = $currentEntry.find('rt');
      for (_i = 0, _len = rts.length; _i < _len; _i++) {
        _rt = rts[_i];
        rt = $(_rt);
        if (rt.data('text') === text) {
          rt.html("&#8203;" + (_.escape(reading)) + "&#8203;");
        }
      }
    }
  };

  setSeparateWords = window.setSeparateWords = function(b) {
    separateWords = b;
  };

  setSeparateSpeaker = window.setSeparateSpeaker = function(b) {
    separateSpeaker = b;
  };

  moveToHistory = function(entry) {
    var historyEntries;
    $history.append(entry);
    historyEntries = $history.find('>.entry');
    if (historyEntries.length > MAX_LOG) {
      return historyEntries.first().remove();
    }
  };

  createNewEntry = function(id) {
    var $res;
    $res = $(("<div class=\"entry\" data-id=\"" + id + "\">") + "<div class=\"parse_frame\"><div class=\"font_zoom\"><div id=\"parse\"></div></div></div>" + "<div class=\"translation_frame\"><div class=\"font_zoom\"><div id=\"translation\"></div></div></div>" + "</div>");
    $current.append($res);
    return $res;
  };

  renderParseResult = function(p) {
    var $block, breakIndex, colorNum, firstChar, firstCharIdx, format, i, inf, isName, j, lastChar, lastPart, okuri, parseResult, parsedClassSuffix, part, reading, readingFormatted, res, stem, text, unparsed, _i, _j, _len, _ref, _ref1;
    parseResult = JSON.parse(p);
    lastParseResult = parseResult;
    res = $("<span>");
    colorNum = 0;
    i = 0;
    format = function(s) {
      var j, q;
      if (!(s != null)) {
        return "";
      }
      q = (function() {
        var _i, _ref, _results;
        _results = [];
        for (j = _i = 0, _ref = s.length; 0 <= _ref ? _i < _ref : _i > _ref; j = 0 <= _ref ? ++_i : --_i) {
          _results.push("<span>" + (_.escape(s.charAt(j))) + "</span>");
        }
        return _results;
      })();
      return q.join('');
    };
    okuri = parseResult.okuri;
    breakIndex = -1;
    if (separateSpeaker) {
      lastPart = parseResult.parts[parseResult.parts.length - 1];
      if (!_.isArray(lastPart)) {
        lastChar = lastPart.charAt(lastPart.length - 1);
        if (lastChar === "』" || lastChar === "」") {
          if (lastChar === "』") {
            firstChar = "『";
          } else {
            firstChar = "「";
          }
          for (j = _i = 0, _ref = parseResult.parts.length; 0 <= _ref ? _i < _ref : _i > _ref; j = 0 <= _ref ? ++_i : --_i) {
            part = parseResult.parts[j];
            if (!_.isArray(part)) {
              firstCharIdx = part.indexOf(firstChar);
              if (firstCharIdx !== -1 && !(j === 0 && firstCharIdx === 0)) {
                breakIndex = j;
                break;
              }
            }
          }
        }
      }
    }
    _ref1 = parseResult.parts;
    for (_j = 0, _len = _ref1.length; _j < _len; _j++) {
      part = _ref1[_j];
      if (_.isArray(part)) {
        text = part[0], stem = part[1], reading = part[2], isName = part[3];
        parsedClassSuffix = isName ? "_name" : colorNum;
        if (!(reading != null) || reading === "") {
          $block = $("<span class=\"noruby\"><span data-num=\"" + i + "\" class=\"text parsed parsed" + parsedClassSuffix + "\"><span class=\"basetext\">" + (format(text)) + "</span></span></span>");
        } else {
          readingFormatted = "&#8203;" + (_.escape(reading)) + "&#8203;";
          if (okuri === "NORMAL") {
            if (text.substr(0, stem.length) !== stem) {
              stem = text;
            }
            inf = text.substr(stem.length);
            $block = $("<span class=\"ruby\"><span data-num=\"" + i + "\" class=\"text parsed parsed" + parsedClassSuffix + "\"><ruby class=\"normal\"><span class=\"basetext\">" + (format(stem)) + "</span><rt>" + readingFormatted + "</rt></ruby><span class=\"basetext\">" + (format(inf)) + "</span></span></span>");
            $block.find('rt').data('text', stem);
          } else {
            $block = $("<span class=\"ruby\"><span data-num=\"" + i + "\" class=\"text parsed parsed" + parsedClassSuffix + "\"><ruby class=\"special\"><span class=\"basetext\">" + (format(text)) + "</span><rt>" + readingFormatted + "</rt></ruby></span></span>");
            $block.find('rt').data('text', text);
          }
        }
        $block.find('.parsed').data('text', text);
        res.append($block);
        colorNum = 1 - colorNum;
      } else {
        unparsed = format(part);
        if (i === breakIndex) {
          unparsed = unparsed.replace(firstChar, '<br />' + firstChar);
        }
        res.append($("<span data-num=\"" + i + "\" class=\"text unparsed\">" + unparsed + "</span>"));
      }
      if (separateWords) {
        res.append("<span>&#8203; &#8203;</span>");
      }
      i += 1;
    }
    return res;
  };

  log = function(s) {
    $('<div>').text(s).appendTo($('#log'));
  };

  flash = window.flash = function(s) {
    $('#flash').finish().text(s).show().fadeOut(3000);
  };

  makePopupSlider = function(slider, trig) {
    var $doc, isMouseDown, isMouseInside;
    isMouseInside = false;
    isMouseDown = false;
    $doc = $(document);
    slider.mouseenter(function() {
      return isMouseInside = true;
    }).mouseleave(function() {
      isMouseInside = false;
      if (!isMouseDown) {
        return slider.hide();
      }
    }).mousedown(function() {
      isMouseDown = true;
      return $doc.one('mouseup', function() {
        isMouseDown = false;
        if (!isMouseInside) {
          return slider.hide();
        }
      });
    });
    trig.mouseenter(function() {
      return slider.show();
    }).mouseleave(function() {
      return setTimeout(function() {
        if (!isMouseDown && !isMouseInside) {
          return slider.hide();
        }
      }, 100);
    });
  };

  setFontSize = function(size) {
    return $('#fontSizeStyle')[0].styleSheet.cssText = ".font_zoom { font-size: " + size + "% }";
  };

}).call(this);
