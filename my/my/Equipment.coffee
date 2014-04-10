require ->
    Slots =
        NORMAL: ['rightHand', 'leftHand', 'chest', 'head', 'boots', 'amulet', 'ring1', 'ring2']

    class Equipment
        @Slots: Slots

        constructor: (@slotNames) ->
            @slots = {}
            @slotNames ?= Slots.NORMAL
