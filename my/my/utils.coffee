do ->
    globals.polyfill = (cls, methods) ->
        for name, val of methods
            if not cls::[name]?
                Object.defineProperty cls::, name,
                    enumerable: false
                    configurable: false
                    writable: false
                    value: val
        return

    polyfill String,
        endsWith: (searchString) ->
            if @length < searchString.length
                false
            else
                @substr(@length - searchString.length) == searchString

        startsWith: (searchString) ->
            if @length < searchString.length
                false
            else
                @substr(0, searchString.length) == searchString

    polyfill Function,
        include: (mixin) ->
            if _.isFunction mixin
                mixin = mixin::
            _.extend @::, mixin
            return

    globals.random = (x) ->
        Math.floor Math.random() * x

    globals.randomBetween = (x, y) ->
        x + random y - x + 1

    globals.randomFrom = (a) ->
        a[random a.length]

    globals.chance = (x) ->
        Math.random() < x

    globals.coinflip = ->
        chance 1/2

    globals.attempt = (maxAttempts, fn) ->
        for i in [0...maxAttempts]
            if fn(i)
                return true
        false

    globals.repeat = (n, fn) ->
        for i in [0...n]
            fn i
        return

    _id = 0
    globals.nextId = ->
        ++_id

    globals.isInt = (n) ->
        d = n - Math.floor(n)
        d <= 1e-3 or (1-d) <= 1e-3

    globals.profile = (id, fn) ->
        s = _.now()
        _ctr = 0
        ctr = ->
            _ctr += 1
        res = fn(ctr)
        f = _.now() - s
        console.log "#{id}: time=#{f} ctr=#{_ctr}"
        res

    globals.sign = (x) ->
        switch
            when x < 0 then -1
            when x > 0 then 1
            else 0

    globals.CanvasExtensions =
        clear: ->
            @clearRect 0, 0, @canvas.width, @canvas.height

    globals.getCanvasContext = (cvs) ->
        cvs = $(cvs)[0]
        ctx = cvs.getContext '2d'
        for k, v of CanvasExtensions
            ctx[k] = v
        ctx

    globals.GenericSet = class GenericSet
        constructor: ->
            @store = {}

        key: ->
            throw new Error "You must override key method!"

        add: (it) ->
            key = @key it
            if key of @store
                false
            else
                @store[key] = it
                true

        has: (it) ->
            @key(it) of @store

        toArray: ->
            _.values @store

    globals.requestAnimationFrame ?= globals.mozRequestAnimationFrame ? globals.webkitRequestAnimationFrame
    startTime = null
    currentAnimation = null
    requestAnimationFrame timing = (t) ->
        if currentAnimation != null
            tmp = currentAnimation
            currentAnimation = null
            tmp t
        startTime = t
        requestAnimationFrame timing

    globals.animate = (duration, animFunc, callback) ->
        start = null
        currentAnimation = fn = (t) ->
            if not start?
                if not startTime?
                    start = t
                else
                    start = startTime
            time = (t - start) / duration
            if time >= 1
                time = 1
                animFunc(time)
                callback() if callback?
            else
                animFunc(time)
                currentAnimation = fn
            return

    globals.interpolate = (x, values) ->
        if values.length <= 1
            values[0]
        else
            x = x * (values.length - 1)
            i = Math.floor(x)
            x = x - i
            if i == values.length - 1
                values[values.length - 1]
            else
                values[i] + x * (values[i + 1] - values[i])

