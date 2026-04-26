# Mythos: chargen UI strings for the Features tab.
humanoid-profile-editor-mythos-features-tab = Features

# Per-slot color slider labels. Generic enough to fit categories with
# different semantics (tail outer/inner, ear inner/outer, breast
# areola/nipple, etc). Per-category overrides could be added later by
# defining mythos-feature-color-slot-{Category}-{slot} keys.
mythos-feature-color-slot-0 = Primary
mythos-feature-color-slot-1 = Secondary
mythos-feature-color-slot-2 = Tertiary
mythos-feature-color-slot-3 = Extra

# Tab titles for chargen feature categories. Loc.TryGetString
# falls back to the raw category string ("Hair", "Tail", ...) when no
# entry exists here, so most categories don't need a key. Eyes is
# special: it's a synthetic tab (eye color picker, not markings) so
# it always needs a localised label.
mythos-feature-category-Eyes = Eyes
mythos-feature-category-FacialHair = Facial Hair
mythos-feature-category-TailFeature = Tail Feature
mythos-feature-category-FaceDetail = Face Detail

# Per-category color slot labels (preserve OV chargen wording).
# mythos-feature-color-slot-{Category}-{slot} overrides the generic
# Primary / Secondary fall-through.
mythos-feature-color-slot-Penis-0 = Member
mythos-feature-color-slot-Penis-1 = Skin
mythos-feature-color-slot-Testicles-0 = Sack
mythos-feature-color-slot-Breasts-0 = Breasts
mythos-feature-color-slot-Vagina-0 = Nethers
mythos-feature-color-slot-Tail-0 = Outer
mythos-feature-color-slot-Tail-1 = Inner
mythos-feature-color-slot-Tail-2 = Tips
mythos-feature-color-slot-Ears-0 = Outer
mythos-feature-color-slot-Ears-1 = Inner

# Synced size slider row (shown for sized categories like Penis,
# Breasts). Per-category override allowed via mythos-feature-size-row-{Category}.
mythos-feature-size-row-default = Size
mythos-feature-size-row-Penis = Length
mythos-feature-size-row-Breasts = Cup size

# Per-feature toggle button labels. Falls back to a title-case of the
# toggle name when no key exists.
mythos-feature-toggle-is_open = Open
mythos-feature-toggle-functional = Functional
mythos-feature-toggle-lactating = Lactating
mythos-feature-toggle-virile = Virile
mythos-feature-toggle-fertility = Fertile

# Synced variant dropdown row. Per-category override allowed via
# mythos-feature-variant-row-{Category}; default is "Type".
mythos-feature-variant-row-default = Type
mythos-feature-variant-row-Penis = Silhouette
mythos-feature-variant-row-Breasts = Arrangement
