(function() {
  var isClipboardTranslation, isDirty, resetOptions, resetOptionsInt, saveOptions, setClipboardTranslation, setDirty;

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
    return resetOptionsInt(host().getOptions());
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

  resetOptionsInt = function(op) {
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
    $('#ok').focus();
    return setDirty(false);
  };

  saveOptions = function() {
    var op;
    if (isDirty) {
      op = {
        clipboard: isClipboardTranslation,
        sentenceDelay: parseInt($('#sentenceDelay').val(), 10)
      };
      host().saveOptions(op);
      return setDirty(false);
    }
  };

}).call(this);
