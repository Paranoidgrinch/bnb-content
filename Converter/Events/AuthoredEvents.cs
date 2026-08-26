namespace BnbContent.Converter.Events;

// Which acts have stopped converting their events and now author them. An act is listed here the moment its
// fifteen are written out of the final master; until then its events still come from the ported JSON, and
// BabLoader still loads that act's event file.
//
// Everything downstream reads the act's events through this one door: the blueprint puts them in its Events
// dictionary, the map draws its Event nodes from their ids, and the presentation manifest names them.
public static class AuthoredEvents
{
    public static IReadOnlyList<BnbEvent> For(int act, ConversionPools pools, Random rng) =>
        act == ActOneEvents.Act ? ActOneEvents.All(pools, rng) : [];
}
