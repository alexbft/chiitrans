require ->
    $.fn.dragging = (options) ->
        options.start ?= -> false
        options.drag ?= ->
        options.end ?= ->
        options.click ?= ->
        @mousedown (e) =>
            if e.which == 1
                e.preventDefault()
                startX = e.pageX
                startY = e.pageY
                isDragging = false
                $doc = $ document
                .one 'mouseup.dragging', (e) ->
                    $doc.off '.dragging'
                    if isDragging
                        options.end e
                    else
                        options.click e, startX, startY
                .on 'mousemove.dragging', (e) ->
                    if not isDragging
                        res = options.start e, startX, startY
                        if res == false
                            $doc.off '.dragging'
                            return
                        isDragging = true
                    options.drag e
            return
        @