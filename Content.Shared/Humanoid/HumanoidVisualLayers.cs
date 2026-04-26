using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid
{
    [Serializable, NetSerializable]
    public enum HumanoidVisualLayers : byte
    {
        Special, // for the cat ears
        Tail,
        TailOverlay, // markings that go ontop of tails
        // Mythos: mutantrace z-zones, one Mythos slot per distinct OV layer
        // constant in code/__DEFINES/misc.dm. MythosBaseSpeciesLayers places
        // each slot at the position equivalent to OV's negated layer value
        // so multi-feature characters render with the same z-relationships
        // as OV. Unused by upstream species (which use BaseSpeciesLayers and
        // never map these slots) — safe no-op there.
        BodyBehind,    // = -BODY_BEHIND_LAYER (-47): catch-all behind-zone slot (markings _behind sprites)
        LLegBehind,    // = -BODY_BEHIND_LAYER (-47): side-view background sprite for the left leg organ
        RLegBehind,    // = -BODY_BEHIND_LAYER (-47): side-view background sprite for the right leg organ
        BodyUnder,     // = -BODY_UNDER_LAYER (-46): reserved
        BodyAdj,       // = -BODY_ADJ_LAYER (-44): catch-all ADJ zone for ears ADJ, head_features, neck_features
        BodyFront,     // = -BODY_FRONT_LAYER (-6):  catch-all FRONT zone for wings FRONT, future generic features
        BodyFronter,   // = -BODY_FRONTER_LAYER (-5): Caustic-only intermediate slot, used by belly FRONT
        BodyFrontest,  // = -BODY_FRONTEST_LAYER (-4): genitals FFRONT (breasts FRONT)
        Hair,
        FacialHair,
        UndergarmentTop,
        UndergarmentBottom,
        Chest,
        Head,
        Snout,
        SnoutCover, // things layered over snouts (i.e. noses)
        HeadSide, // side parts (i.e., frills)
        HeadTop,  // top parts (i.e., ears)
        Eyes,
        RArm,
        LArm,
        RHand,
        LHand,
        RLeg,
        LLeg,
        RFoot,
        LFoot,
        Overlay,
        Handcuffs,
        StencilMask,
        Ensnare,
        Fire,

    }
}
