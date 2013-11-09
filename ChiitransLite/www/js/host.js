(function() {
  var applyTheme, callbacks, createMethod, host, hostCallback, initHost, qid, query, _host,
    __slice = [].slice;

  qid = new Date().getTime();

  _host = null;

  callbacks = {};

  initHost = function() {
    var m, methods, _i, _len, _results;
    methods = JSON.parse(external.getMethods());
    _host = {};
    _results = [];
    for (_i = 0, _len = methods.length; _i < _len; _i++) {
      m = methods[_i];
      _results.push(_host[m] = createMethod(m));
    }
    return _results;
  };

  createMethod = function(methodName) {
    return function() {
      var args, cb, isInline, n;
      args = 1 <= arguments.length ? __slice.call(arguments, 0) : [];
      n = args.length;
      if (n > 0 && _.isFunction(args[n - 1])) {
        cb = args[n - 1];
        args = args.slice(0, n - 1);
        isInline = false;
      } else {
        cb = null;
        isInline = true;
      }
      return query(methodName, args, cb, isInline);
    };
  };

  query = function(methodName, args, cb, isInline) {
    var queryId, resJson;
    if (isInline) {
      resJson = external.query(methodName, JSON.stringify(args), 0);
      return JSON.parse(resJson);
    } else {
      queryId = qid++;
      callbacks[queryId] = cb;
      external.query(methodName, JSON.stringify(args), queryId);
      return true;
    }
  };

  host = window.host = function() {
    if (!_host) {
      initHost();
    }
    return _host;
  };

  hostCallback = window.hostCallback = function(queryId, responseJSON) {
    var cb, qq;
    qq = "" + queryId;
    if (callbacks[qq] != null) {
      cb = callbacks[qq];
      delete callbacks[qq];
      cb(JSON.parse(responseJSON));
    }
  };

  if (navigator.userAgent.indexOf("MSIE" === -1)) {
    $('html').addClass('ie11');
  }

  applyTheme = window.applyTheme = function(theme) {
    $('link#currentTheme').remove();
    if (theme) {
      $('head').append("<link id=\"currentTheme\" rel=\"stylesheet\" href=\"themes/" + theme + ".css\" />");
    }
  };

  window.document_attachEvent = function(ev, cb) {
    if (document.attachEvent != null) {
      return document.attachEvent(ev, cb);
    } else {
      return document.addEventListener(ev.substr(2), cb, false);
    }
  };

}).call(this);
