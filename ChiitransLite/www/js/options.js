(function() {
  var getRadioValue, isClipboardTranslation, isDirty, resetOptions, resetOptionsInt, saveOptions, selectTab, setClipboardTranslation, setDirty, setRadioValue;

  isDirty = false;

  isClipboardTranslation = false;

  $(function() {
    $('input[type=radio], input[type=checkbox], input[type=text]').change(function() {
      return setDirty(true);
    });
    $('#ok').click(function() {
      saveOptions();
      return host().close();
    });
    $('#apply').click(function() {
      return saveOptions();
    });
    $('#cancel').click(function() {
      return host().close();
    });
    $('#clipboard').click(function() {
      setClipboardTranslation(!isClipboardTranslation);
      return setDirty(true);
    });
    $('#showHooks').click(function() {
      return host().showHookForm();
    });
    $('#showPoFiles').click(function() {
      return host().showPoFiles(function() {});
    });
    $('#theme').change(function() {
      return setDirty(true);
    });
    $('#reset').click(function() {
      return host().resetParsePreferences();
    });
    resetOptionsInt(host().getOptions());
    $('.menu>li').click(function() {
      return selectTab($(this));
    });
    return selectTab($('.menu li:first'));
  });

  resetOptions = window.resetOptions = function(opJson) {
    var op;
    op = JSON.parse(opJson);
    $(function() {
      return resetOptionsInt(op);
    });
  };

  setDirty = function(_isDirty) {
    isDirty = _isDirty;
    $('#apply').prop('disabled', !isDirty);
  };

  setClipboardTranslation = function(isEnabled) {
    isClipboardTranslation = isEnabled;
    if (isEnabled) {
      $('#clipboard').addClass('pressed').attr('title', 'Disable Capture text from Clipboard');
    } else {
      $('#clipboard').removeClass('pressed').attr('title', 'Enable Capture text from Clipboard');
    }
  };

  setRadioValue = function(name, value) {
    return $("input[type=radio][name=\"" + name + "\"][value=\"" + value + "\"]").prop('checked', true);
  };

  getRadioValue = function(name) {
    return $("input[type=radio][name=\"" + name + "\"]:checked").val();
  };

  resetOptionsInt = function(op) {
    var $theme, theme, _i, _len, _ref, _ref1;
    setClipboardTranslation(op.clipboard);
    $('#sentenceDelay').val(op.sentenceDelay);
    $('.btn').addClass('enabled');
    $('#showHooks').prop('disabled', !op.enableHooks);
    if (!op.enableHooks) {
      $('#showHooks').removeClass('enabled');
    }
    $('#sentenceDelay').prop('disabled', !op.enableSentenceDelay);
    if (!op.enableSentenceDelay) {
      $('#sentenceDelay').removeClass('enabled');
    }
    setRadioValue("display", op.display);
    setRadioValue("okuri", op.okuri);
    $theme = $('#theme');
    $theme.html("<option value=\"\">Default</option>");
    _ref = op.themes;
    for (_i = 0, _len = _ref.length; _i < _len; _i++) {
      theme = _ref[_i];
      $theme.append("<option value=\"" + (_.escape(theme)) + "\">" + (_.escape(theme)) + "</option>");
    }
    $theme.val((_ref1 = op.theme) != null ? _ref1 : "");
    $('#separateWords').prop('checked', op.separateWords);
    $('#separateSpeaker').prop('checked', op.separateSpeaker);
    $('#ok').focus();
    return setDirty(false);
  };

  saveOptions = function() {
    var op;
    if (isDirty) {
      op = {
        clipboard: isClipboardTranslation,
        sentenceDelay: parseInt($('#sentenceDelay').val(), 10),
        display: getRadioValue("display"),
        okuri: getRadioValue("okuri"),
        theme: $('#theme').val(),
        separateWords: $('#separateWords').prop('checked'),
        separateSpeaker: $('#separateSpeaker').prop('checked')
      };
      host().saveOptions(op);
      return setDirty(false);
    }
  };

  selectTab = function(li) {
    var tabId;
    $('.menu>li').removeClass('active');
    $('.tab').removeClass('active');
    li.addClass('active');
    tabId = li.data('target');
    $("#" + tabId).addClass('active');
  };

}).call(this);
