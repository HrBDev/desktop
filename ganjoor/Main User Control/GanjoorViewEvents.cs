﻿namespace ganjoor
{
    public delegate void PageChangedEvent(string PageString, bool HasComments, bool CanBrowse, bool IsFaved, bool FavsPage, string highlightedText, object preItem, object nextItem);
}
