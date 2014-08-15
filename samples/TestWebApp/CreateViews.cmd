for %%1 in (Create Edit Delete List Details) do (
    k gen view %%1_DefaultLayout %%1 -m Person -dc PersonContext -udl -scripts -f
    k gen view %%1_DefaultLayoutPartial %%1 -m Person -dc PersonContext -udl -partial -scripts -f
    k gen view %%1_CustomLayout %%1 -m Person -dc PersonContext -l "MyLayout.cshtml" -scripts -f
    k gen view %%1_CustomLayoutPartial %%1 -m Person -dc PersonContext -l "MyLayout.cshtml" -partial -scripts -f
    k gen view %%1_NoLayout %%1 -m Person -dc PersonContext -scripts -f
    k gen view %%1_NoLayoutPartial %%1 -m Person -dc PersonContext -partial -scripts -f
)