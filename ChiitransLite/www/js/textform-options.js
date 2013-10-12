(function() {
  var getRadioValue, isDirty, resetOptions, resetOptionsInt, saveOptions, setDirty, setRadioValue;

  isDirty = false;

  $(function() {
    $('input[type=radio], input[type=checkbox]').change(function() {
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
    $('#theme').change(function() {
      return setDirty(true);
    });
    $('#reset').click(function() {
      return host().resetParsePreferences();
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

  setRadioValue = function(name, value) {
    return $("input[type=radio][name=\"" + name + "\"][value=\"" + value + "\"]").prop('checked', true);
  };

  getRadioValue = function(name) {
    return $("input[type=radio][name=\"" + name + "\"]:checked").val();
  };

  resetOptionsInt = function(op) {
    var $theme, theme, _i, _len, _ref, _ref1;
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
    $('#reset').blur();
    $('#ok').focus();
    return setDirty(false);
  };

  saveOptions = function() {
    var op;
    if (isDirty) {
      op = {
        display: getRadioValue("display"),
        okuri: getRadioValue("okuri"),
        theme: $('#theme').val(),
        separateWords: $('#separateWords').prop('checked')
      };
      host().saveOptions(op);
      return setDirty(false);
    }
  };

}).call(this);
