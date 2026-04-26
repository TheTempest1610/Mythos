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
