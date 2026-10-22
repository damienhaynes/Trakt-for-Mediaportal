using MediaPortal.GUI.Library;
using MediaPortal.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using TraktAPI.DataModels;
using TraktAPI.Extensions;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataModels;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
  public class GUIFavoritedShows : GUIWindow
  {
    #region Skin Controls

    [SkinControl( 2 )]
    protected GUIButtonControl layoutButton = null;

    [SkinControl( 8 )]
    protected GUISortButtonControl sortButton = null;

    [SkinControl( 9 )]
    protected GUICheckButton filterWatchedButton = null;

    [SkinControl( 10 )]
    protected GUICheckButton filterWatchListedButton = null;

    [SkinControl( 11 )]
    protected GUICheckButton filterCollectedButton = null;

    [SkinControl( 12 )]
    protected GUICheckButton filterRatedButton = null;

    [SkinControl( 13 )]
    protected GUIButtonControl periodButton = null;

    [SkinControl( 50 )]
    protected GUIFacadeControl Facade = null;

    [SkinControlAttribute( 60 )]
    protected GUIImage FanartBackground = null;

    [SkinControlAttribute( 61 )]
    protected GUIImage FanartBackground2 = null;

    [SkinControlAttribute( 62 )]
    protected GUIImage loadingImage = null;

    #endregion

    #region Enums

    #endregion

    #region Constructor

    public GUIFavoritedShows()
    {
      backdrop = new ImageSwapper
      {
        PropertyOne = "#Trakt.FavoritedShows.Fanart.1",
        PropertyTwo = "#Trakt.FavoritedShows.Fanart.2"
      };
    }

    #endregion

    #region Private Variables

    private Dictionary<int, TraktShowsFavorited> FavoritedShowPages = null;
    private GUIFacadeControl.Layout CurrentLayout { get; set; }
    private readonly ImageSwapper backdrop;
    DateTime LastRequest = new DateTime();
    int PreviousSelectedIndex = 0;
    int CurrentPage = 1;

    #endregion

    #region Base Overrides

    public override int GetID
    {
      get
      {
        return (int)TraktGUIWindows.FavoritedShows;
      }
    }

    public override bool Init()
    {
      return Load( GUIGraphicsContext.Skin + @"\Trakt.Favorited.Shows.xml" );
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();

      // Clear GUI Properties
      ClearProperties();

      // Init Properties
      InitProperties();

      // Load Favorited Shows
      LoadFavoritedShows( CurrentPage );
    }

    protected override void OnPageDestroy( int new_windowId )
    {
      GUIShowListItem.StopDownload = true;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;
      ClearProperties();

      // save current layout
      TraktSettings.FavoritedShowsDefaultLayout = (int)CurrentLayout;

      base.OnPageDestroy( new_windowId );
    }

    protected override void OnClicked( int controlId, GUIControl control, Action.ActionType actionType )
    {
      // wait for any background action to finish
      if ( GUIBackgroundTask.Instance.IsBusy )
        return;

      switch ( controlId )
      {
        // Facade
        case ( 50 ):
          if ( actionType == Action.ActionType.ACTION_SELECT_ITEM )
          {
            if ( !( Facade.SelectedListItem is GUIShowListItem item ) )
              return;

            if ( !item.IsFolder )
            {
              if ( TraktSettings.EnableJumpToForTVShows )
              {
                CheckAndPlayEpisode( true );
              }
              else
              {
                if ( item.Show == null )
                  return;

                GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.ShowSeasons, item.Show.ToJSON() );
              }
            }
            else
            {
              if ( item.IsPrevPageItem )
                CurrentPage--;
              else
                CurrentPage++;

              if ( CurrentPage == 1 )
                PreviousSelectedIndex = 0;
              else
                PreviousSelectedIndex = 1;

              // load next / previous page
              LoadFavoritedShows( CurrentPage );
            }
          }
          break;

        // Layout Button
        case ( 2 ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        // Sort Button
        case ( 8 ):
          var newSortBy = GUICommon.ShowSortMenu( TraktSettings.SortByFavoritedShows );
          if ( newSortBy != null )
          {
            if ( newSortBy.Field != TraktSettings.SortByFavoritedShows.Field )
            {
              TraktSettings.SortByFavoritedShows = newSortBy;
              PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
              UpdateButtonState();
              LoadFavoritedShows( CurrentPage );
            }
          }
          break;

        // Hide Watched
        case ( 9 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedShowsHideWatched = !TraktSettings.FavoritedShowsHideWatched;
          UpdateButtonState();
          LoadFavoritedShows( CurrentPage );
          break;

        // Hide Watchlisted
        case ( 10 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedShowsHideWatchlisted = !TraktSettings.FavoritedShowsHideWatchlisted;
          UpdateButtonState();
          LoadFavoritedShows( CurrentPage );
          break;

        // Hide Collected
        case ( 11 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedShowsHideCollected = !TraktSettings.FavoritedShowsHideCollected;
          UpdateButtonState();
          LoadFavoritedShows( CurrentPage );
          break;

        // Hide Rated
        case ( 12 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedShowsHideRated = !TraktSettings.FavoritedShowsHideRated;
          UpdateButtonState();
          LoadFavoritedShows( CurrentPage );
          break;

        // Time Period Button
        case ( 13 ):
          var newPeriod = GUICommon.ShowFavoritedPeriodMenu( TraktSettings.FavoritedShowsPeriod );
          if ( newPeriod != TraktSettings.FavoritedShowsPeriod )
          {
            TraktSettings.FavoritedShowsPeriod = newPeriod;
            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
            FavoritedShowPages = null;
            UpdateButtonState();
            LoadFavoritedShows( CurrentPage );
          }
          break;

        default:
          break;
      }
      base.OnClicked( controlId, control, actionType );
    }

    public override void OnAction( Action action )
    {
      switch ( action.wID )
      {
        case Action.ActionType.ACTION_PLAY:
        case Action.ActionType.ACTION_MUSIC_PLAY:
          CheckAndPlayEpisode( false );
          break;
        default:
          base.OnAction( action );
          break;
      }
    }

    protected override void OnShowContextMenu()
    {
      var selectedItem = this.Facade.SelectedListItem as GUIShowListItem;
      if ( selectedItem == null )
        return;

      var selectedFavoritedItem = selectedItem.TVTag as TraktShowFavorited;
      if ( selectedFavoritedItem == null )
        return;

      var dlg = (IDialogbox)GUIWindowManager.GetWindow( (int)GUIWindow.Window.WINDOW_DIALOG_MENU );
      if ( dlg == null )
        return;

      dlg.Reset();
      dlg.SetHeading( GUIUtils.PluginName() );

      GUICommon.CreateShowsContextMenu( ref dlg, selectedFavoritedItem.Show, false );

      // Show Context Menu
      dlg.DoModal( GUIWindowManager.ActiveWindow );
      if ( dlg.SelectedId < 0 )
        return;

      switch ( dlg.SelectedId )
      {
        case ( (int)MediaContextMenuItem.ShowSeasonInfo ):
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.ShowSeasons, selectedFavoritedItem.Show.ToJSON() );
          break;

        case ( (int)MediaContextMenuItem.MarkAsWatched ):
          GUICommon.MarkShowAsWatched( selectedFavoritedItem.Show );
          selectedItem.IsPlayed = true;
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedShowsHideWatched )
            LoadFavoritedShows( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.AddToWatchList ):
          TraktHelper.AddShowToWatchList( selectedFavoritedItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedShowsHideWatchlisted )
            LoadFavoritedShows( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.RemoveFromWatchList ):
          TraktHelper.RemoveShowFromWatchList( selectedFavoritedItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.AddToFavorites ):
          TraktHelper.AddShowToFavorites( selectedFavoritedItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.RemoveFromFavorites ):
          TraktHelper.RemoveShowFromFavorites( selectedFavoritedItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.AddToList ):
          TraktHelper.AddRemoveShowInUserList( selectedFavoritedItem.Show, false );
          break;

        case ( (int)MediaContextMenuItem.AddToLibrary ):
          GUICommon.AddShowToCollection( selectedFavoritedItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedShowsHideCollected )
            LoadFavoritedShows( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.Related ):
          TraktHelper.ShowRelatedShows( selectedFavoritedItem.Show );
          break;

        case ( (int)MediaContextMenuItem.Rate ):
          GUICommon.RateShow( selectedFavoritedItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedShowsHideRated )
            LoadFavoritedShows( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.Filters ):
          if ( GUICommon.ShowTVShowFiltersMenu() )
          {
            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
            UpdateButtonState();
            LoadFavoritedShows( CurrentPage );
          }
          break;

        case ( (int)MediaContextMenuItem.Shouts ):
          TraktHelper.ShowTVShowShouts( selectedFavoritedItem.Show );
          break;

        case ( (int)MediaContextMenuItem.Cast ):
          GUICreditsShow.Show = selectedFavoritedItem.Show;
          GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
          GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename( selectedItem.Images.ShowImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsShow );
          break;

        case ( (int)MediaContextMenuItem.Crew ):
          GUICreditsShow.Show = selectedFavoritedItem.Show;
          GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
          GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename( selectedItem.Images.ShowImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsShow );
          break;

        case ( (int)MediaContextMenuItem.Trailers ):
          GUICommon.ShowTVShowTrailersMenu( selectedFavoritedItem.Show );
          break;

        case ( (int)MediaContextMenuItem.ChangeLayout ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        case ( (int)MediaContextMenuItem.SearchWithMpNZB ):
          string loadingParam = string.Format( "search:{0}", selectedFavoritedItem.Show.Title );
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MpNZB, loadingParam );
          break;

        case ( (int)MediaContextMenuItem.SearchTorrent ):
          string loadPar = selectedFavoritedItem.Show.Title;
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MyTorrents, loadPar );
          break;

        default:
          break;
      }

      base.OnShowContextMenu();
    }

    #endregion

    #region Private Methods

    TraktShowsFavorited GetFavoritedShows( int page )
    {
      TraktShowsFavorited favoritedShows;

      if ( FavoritedShowPages == null || LastRequest < DateTime.UtcNow.Subtract( new TimeSpan( 0, TraktSettings.WebRequestCacheMinutes, 0 ) ) )
      {
        // get the first page
        favoritedShows = TraktAPI.TraktAPI.GetFavoritedShows( period: TraktSettings.FavoritedShowsPeriod, page: 1, maxItems: TraktSettings.MaxFavoritedShowsRequest );

        // reset to defaults
        LastRequest = DateTime.UtcNow;
        CurrentPage = 1;
        PreviousSelectedIndex = 0;

        // clear the cache
        if ( FavoritedShowPages == null )
          FavoritedShowPages = new Dictionary<int, TraktShowsFavorited>();
        else
          FavoritedShowPages.Clear();

        // add page to cache
        FavoritedShowPages.Add( 1, favoritedShows );
      }
      else
      {
        // get page from cache if it exists
        if ( FavoritedShowPages.TryGetValue( page, out favoritedShows ) )
        {
          return favoritedShows;
        }

        // request next page
        favoritedShows = TraktAPI.TraktAPI.GetFavoritedShows( period: TraktSettings.FavoritedShowsPeriod, page: page, maxItems: TraktSettings.MaxFavoritedShowsRequest );
        if ( favoritedShows != null && favoritedShows.Shows != null )
        {
          // add to cache
          FavoritedShowPages.Add( page, favoritedShows );
        }
      }

      return favoritedShows;
    }

    private void CheckAndPlayEpisode( bool jumpTo )
    {
      var selectedItem = this.Facade.SelectedListItem;
      if ( selectedItem == null )
        return;

      var selectedFavoritedItem = selectedItem.TVTag as TraktShowFavorited;
      if ( selectedFavoritedItem == null )
        return;

      GUICommon.CheckAndPlayFirstUnwatchedEpisode( selectedFavoritedItem.Show, jumpTo );
    }

    private void LoadFavoritedShows( int page = 1 )
    {
      GUIUtils.SetProperty( "#Trakt.Items", string.Empty );

      GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback( () =>
      {
        return GetFavoritedShows( page );
      },
      delegate ( bool success, object result )
      {
        if ( success )
        {
          var shows = result as TraktShowsFavorited;
          SendFavoritedShowsToFacade( shows );
        }
      }, Translation.GettingFavoritedShows, true );
    }

    private void SendFavoritedShowsToFacade( TraktShowsFavorited favoritedItems )
    {
      // clear facade
      GUIControl.ClearControl( GetID, Facade.GetID );

      if ( favoritedItems == null )
      {
        GUIUtils.ShowNotifyDialog( Translation.Error, Translation.ErrorGeneral );
        GUIWindowManager.ShowPreviousWindow();
        FavoritedShowPages = null;
        return;
      }

      if ( favoritedItems.Shows.Count() == 0 )
      {
        GUIUtils.ShowNotifyDialog( GUIUtils.PluginName(), Translation.NoFavoritedShows );
        GUIWindowManager.ShowPreviousWindow();
        FavoritedShowPages = null;
        return;
      }

      // filter shows
      var filteredFavoritedList = GUICommon.FilterFavoritedShows( favoritedItems.Shows ).Where( s => !string.IsNullOrEmpty( s.Show.Title ) ).ToList();

      // sort shows
      filteredFavoritedList.Sort( new GUIListItemShowSorter( TraktSettings.SortByFavoritedShows.Field, TraktSettings.SortByFavoritedShows.Direction ) );

      int itemId = 0;
      var showImages = new List<GUITmdbImage>();

      // Add Previous Page Button
      if ( favoritedItems.CurrentPage != 1 )
      {
        var prevPageItem = new GUIShowListItem( Translation.PreviousPage, (int)TraktGUIWindows.FavoritedShows )
        {
          IsPrevPageItem = true,
          IconImage = "traktPreviousPage.png",
          IconImageBig = "traktPreviousPage.png",
          ThumbnailImage = "traktPreviousPage.png"
        };
        prevPageItem.OnItemSelected += OnPreviousPageSelected;
        prevPageItem.IsFolder = true;
        Facade.Add( prevPageItem );
        itemId++;
      }

      // Add each show mark, remote if not in collection            
      foreach ( var favoritedItem in filteredFavoritedList )
      {
        // add image for download
        var images = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = favoritedItem.Show.Ids.Tmdb } };
        showImages.Add( images );

        var item = new GUIShowListItem( favoritedItem.Show.Title, (int)TraktGUIWindows.FavoritedShows )
        {
          Label2 = favoritedItem.Show.Year == null ? "----" : favoritedItem.Show.Year.ToString(),
          TVTag = favoritedItem,
          Show = favoritedItem.Show,
          Images = images,
          IsPlayed = favoritedItem.Show.IsWatched(),
          ItemId = Int32.MaxValue - itemId,
          IconImage = GUIImageHandler.GetDefaultPoster( false ),
          IconImageBig = GUIImageHandler.GetDefaultPoster(),
          ThumbnailImage = GUIImageHandler.GetDefaultPoster()
        };

        item.OnItemSelected += OnShowSelected;
        Utils.SetDefaultIcons( item );
        Facade.Add( item );
        itemId++;
      }

      // Add Next Page Button
      if ( favoritedItems.CurrentPage != favoritedItems.TotalPages )
      {
        var nextPageItem = new GUIShowListItem( Translation.NextPage, (int)TraktGUIWindows.FavoritedShows )
        {
          IsNextPageItem = true,
          IconImage = "traktNextPage.png",
          IconImageBig = "traktNextPage.png",
          ThumbnailImage = "traktNextPage.png"
        };
        nextPageItem.OnItemSelected += OnNextPageSelected;
        nextPageItem.IsFolder = true;
        Facade.Add( nextPageItem );
        itemId++;
      }

      // Set Facade Layout
      Facade.CurrentLayout = CurrentLayout;
      GUIControl.FocusControl( GetID, Facade.GetID );

      Facade.SelectIndex( PreviousSelectedIndex );

      // set facade properties
      GUIUtils.SetProperty( "#itemcount", filteredFavoritedList.Count().ToString() );
      GUIUtils.SetProperty( "#Trakt.Items", string.Format( "{0} {1}", filteredFavoritedList.Count(), filteredFavoritedList.Count() > 1 ? Translation.Shows : Translation.Show ) );

      // Page Properties
      GUIUtils.SetProperty( "#Trakt.Facade.CurrentPage", favoritedItems.CurrentPage.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.TotalPages", favoritedItems.TotalPages.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxFavoritedShowsRequest.ToString() );

      // Download show images Async and set to facade
      GUIShowListItem.GetImages( showImages );
    }

    private void InitProperties()
    {
      // Fanart
      backdrop.GUIImageOne = FanartBackground;
      backdrop.GUIImageTwo = FanartBackground2;
      backdrop.LoadingImage = loadingImage;

      // load last layout
      CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.FavoritedShowsDefaultLayout;

      // Update Button States
      UpdateButtonState();

      if ( sortButton != null )
      {
        UpdateButtonState();
        sortButton.SortChanged += ( o, e ) =>
        {
          TraktSettings.SortByFavoritedShows.Direction = (SortingDirections)( e.Order - 1 );
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          UpdateButtonState();
          LoadFavoritedShows( CurrentPage );
        };
      }
    }

    private void UpdateButtonState()
    {
      // update layout button label
      GUIControl.SetControlLabel( GetID, layoutButton.GetID, GUICommon.GetLayoutTranslation( CurrentLayout ) );

      // update sortby button label
      if ( sortButton != null )
      {
        sortButton.Label = GUICommon.GetSortByString( TraktSettings.SortByFavoritedShows );
        sortButton.IsAscending = ( TraktSettings.SortByFavoritedShows.Direction == SortingDirections.Ascending );
      }

      if ( periodButton != null )
      {
        periodButton.Label = GUICommon.GetPeriodString( TraktSettings.FavoritedShowsPeriod );
      }

      GUIUtils.SetProperty( "#Trakt.FavoritedShows.Period", GUICommon.GetTranslatedFavoritedPeriod( TraktSettings.FavoritedShowsPeriod ) );
      GUIUtils.SetProperty( "#Trakt.SortBy", GUICommon.GetSortByString( TraktSettings.SortByFavoritedShows ) );

      // update filter buttons
      if ( filterWatchedButton != null )
        filterWatchedButton.Selected = TraktSettings.FavoritedShowsHideWatched;
      if ( filterWatchListedButton != null )
        filterWatchListedButton.Selected = TraktSettings.FavoritedShowsHideWatchlisted;
      if ( filterCollectedButton != null )
        filterCollectedButton.Selected = TraktSettings.FavoritedShowsHideCollected;
      if ( filterRatedButton != null )
        filterRatedButton.Selected = TraktSettings.FavoritedShowsHideRated;
    }

    private void ClearProperties( bool showsOnly = false )
    {
      if ( !showsOnly )
      {
        GUIUtils.SetProperty( "#Trakt.FavoritedShows.Period", string.Empty );
        GUIUtils.SetProperty( "#Trakt.FavoritedShows.CurrentPage", string.Empty );
        GUIUtils.SetProperty( "#Trakt.FavoritedShows.TotalPages", string.Empty );
        GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", string.Empty );
      }

      GUIUtils.SetProperty( "#Trakt.Show.UserCount", string.Empty );

      GUICommon.ClearShowProperties();
    }

    private void PublishShowSkinProperties( TraktShowFavorited favoritedItem )
    {
      GUICommon.SetProperty( "#Trakt.Show.UserCount", favoritedItem.UserCount.ToString() );

      GUICommon.SetShowProperties( favoritedItem.Show );
    }

    private void OnShowSelected( GUIListItem item, GUIControl control )
    {
      GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", false.ToString() );

      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      var favoritedItem = item.TVTag as TraktShowFavorited;
      if ( favoritedItem == null )
        return;

      PublishShowSkinProperties( favoritedItem );
      GUIImageHandler.LoadFanart( backdrop, TmdbCache.GetShowBackdropFilename( ( item as GUIShowListItem ).Images.ShowImages ) );
    }

    private void OnNextPageSelected( GUIListItem item, GUIControl control )
    {
      GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", true.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.PageToLoad", ( CurrentPage + 1 ).ToString() );

      backdrop.Filename = string.Empty;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      // only clear the last selected show properties
      ClearProperties( true );
    }

    private void OnPreviousPageSelected( GUIListItem item, GUIControl control )
    {
      GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", true.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.PageToLoad", ( CurrentPage - 1 ).ToString() );

      backdrop.Filename = string.Empty;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      // only clear the last selected show properties
      ClearProperties( true );
    }

    #endregion
  }
}
