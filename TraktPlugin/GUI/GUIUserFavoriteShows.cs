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
  public class GUIUserFavoriteShows : GUIWindow
  {
    #region Skin Controls

    [SkinControl( 2 )]
    protected GUIButtonControl layoutButton = null;

    [SkinControl( 8 )]
    protected GUISortButtonControl sortButton = null;

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

    enum ContextMenuItem
    {
      ShowSeasonInfo,
      RemoveFromFavorites,
      AddToFavorites,
      RemoveFromWatchList,
      AddToWatchList,
      AddToList,
      Trailers,
      Related,
      Rate,
      Shouts,
      Cast,
      Crew,
      ChangeLayout,
      SearchWithMpNZB,
      SearchTorrent
    }

    #endregion

    #region Constructor

    public GUIUserFavoriteShows()
    {
      backdrop = new ImageSwapper
      {
        PropertyOne = "#Trakt.UserFavoriteShows.Fanart.1",
        PropertyTwo = "#Trakt.UserFavoriteShows.Fanart.2"
      };
    }

    #endregion

    #region Private Variables

    private GUIFacadeControl.Layout CurrentLayout { get; set; }
    static int PreviousSelectedIndex { get; set; }
    private readonly ImageSwapper backdrop;
    static DateTime LastRequest = new DateTime();
    static readonly Dictionary<string, IEnumerable<TraktFavoriteItem>> userFavorites = new Dictionary<string, IEnumerable<TraktFavoriteItem>>();

    static IEnumerable<TraktFavoriteItem> FavoriteShows
    {
      get
      {
        if ( !userFavorites.Keys.Contains( CurrentUser ) || LastRequest < DateTime.UtcNow.Subtract( new TimeSpan( 0, TraktSettings.WebRequestCacheMinutes, 0 ) ) )
        {
          string username = CurrentUser == TraktSettings.Username ? "me" : CurrentUser;

          // NB: since we're returning all items there is no need to use the sortby API parameters for each page request
          int maxItemsPerPage = 100;
          TraktFavoriteItems favoriteItems = TraktAPI.TraktAPI.GetFavourites( username, type: "shows", extendedInfoParams: "full", page: 1, maxItems: maxItemsPerPage );

          if ( favoriteItems == null || favoriteItems.Items == null )
          {
            userFavorites.Remove( CurrentUser );
            return null;
          }

          _FavoriteShows = favoriteItems.Items;

          // get next page(s) if required
          while ( favoriteItems.CurrentPage < favoriteItems.TotalPages )
          {
            // Note: API returns total pages for all watchlist types not just this one (shows)
            // so we need to check returned items against our expected max items per page
            if ( _FavoriteShows.Count() < ( maxItemsPerPage * favoriteItems.CurrentPage ) )
              break;

            favoriteItems = TraktAPI.TraktAPI.GetFavourites( username, type: "shows", extendedInfoParams: "full", page: favoriteItems.CurrentPage + 1, maxItems: maxItemsPerPage );
            if ( favoriteItems == null || favoriteItems.Items == null )
              break;

            _FavoriteShows = _FavoriteShows.Concat( favoriteItems.Items );
          }

          if ( userFavorites.Keys.Contains( CurrentUser ) )
            userFavorites.Remove( CurrentUser );

          userFavorites.Add( CurrentUser, _FavoriteShows );
          LastRequest = DateTime.UtcNow;
          PreviousSelectedIndex = 0;
        }

        return userFavorites[ CurrentUser ];
      }
    }
    static IEnumerable<TraktFavoriteItem> _FavoriteShows = null;

    #endregion

    #region Public Properties

    public static string CurrentUser { get; set; }

    #endregion

    #region Base Overrides

    public override int GetID
    {
      get
      {
        return (int)TraktGUIWindows.UserFavoriteShows;
      }
    }

    public override bool Init()
    {
      return Load( GUIGraphicsContext.Skin + @"\Trakt.UserFavorite.Shows.xml" );
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();

      // Clear GUI Properties
      ClearProperties();

      // Requires Login
      if ( !GUICommon.CheckLogin() )
        return;

      // Init Properties
      InitProperties();

      // Load Favorite Shows
      LoadFavoriteShows();
    }

    protected override void OnPageDestroy( int new_windowId )
    {
      GUIShowListItem.StopDownload = true;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;
      ClearProperties();

      // save current layout
      TraktSettings.UserFavoriteShowsDefaultLayout = (int)CurrentLayout;

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
            if ( TraktSettings.EnableJumpToForTVShows )
            {
              CheckAndPlayEpisode( true );
            }
            else
            {
              if ( !( Facade.SelectedListItem is GUIShowListItem item ) )
                return;

              if ( item.Show == null )
                return;

              GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.ShowSeasons, item.Show.ToJSON() );
            }
          }
          break;

        // Layout Button
        case ( 2 ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        // Sort Button
        case ( 8 ):
          var newSortBy = GUICommon.ShowSortMenu( TraktSettings.SortByUserFavoriteShows );
          if ( newSortBy != null )
          {
            if ( newSortBy.Field != TraktSettings.SortByUserFavoriteShows.Field )
            {
              TraktSettings.SortByUserFavoriteShows = newSortBy;
              PreviousSelectedIndex = 0;
              UpdateButtonState();
              LoadFavoriteShows();
            }
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
        case Action.ActionType.ACTION_PREVIOUS_MENU:
          // restore current user
          CurrentUser = TraktSettings.Username;
          base.OnAction( action );
          break;
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

      var selectedFavoriteItem = selectedItem.TVTag as TraktFavoriteItem;
      if ( selectedFavoriteItem == null )
        return;

      var dlg = (IDialogbox)GUIWindowManager.GetWindow( (int)GUIWindow.Window.WINDOW_DIALOG_MENU );
      if ( dlg == null )
        return;

      dlg.Reset();
      dlg.SetHeading( GUIUtils.PluginName() );

      GUIListItem listItem;

      // Show Season Information
      listItem = new GUIListItem( Translation.ShowSeasonInfo );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.ShowSeasonInfo;

      // Add to Favorites / Remove from Favorites
      // only allow removal if viewing your own favorites
      if ( CurrentUser == TraktSettings.Username )
      {
        listItem = new GUIListItem( Translation.RemoveFromFavorites );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.RemoveFromFavorites;
      }
      else if ( !selectedFavoriteItem.Show.IsFavorited() )
      {
        // viewing someone else's favorites and not in yours
        listItem = new GUIListItem( Translation.AddToFavorites );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.AddToFavorites;
      }

      // Add to Watchlist
      if ( !selectedFavoriteItem.Show.IsWatchlisted() )
      {
        listItem = new GUIListItem( Translation.AddToWatchList );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.AddToWatchList;
      }
      else
      {
        listItem = new GUIListItem( Translation.RemoveFromWatchList );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.RemoveFromWatchList;
      }

      // Add to Custom List
      listItem = new GUIListItem( Translation.AddToList );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.AddToList;

      // Related Shows
      listItem = new GUIListItem( Translation.RelatedShows );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.Related;

      // Rate Show
      listItem = new GUIListItem( Translation.RateShow );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.Rate;

      // Shouts
      listItem = new GUIListItem( Translation.Comments );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.Shouts;

      // Cast and Crew
      listItem = new GUIListItem( Translation.Cast );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.Cast;

      listItem = new GUIListItem( Translation.Crew );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.Crew;

      // Change Layout
      listItem = new GUIListItem( Translation.ChangeLayout );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.ChangeLayout;

      // Trailers
      if ( TraktHelper.IsTrailersAvailableAndEnabled )
      {
        // Trailers
        listItem = new GUIListItem( Translation.Trailers );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.Trailers;
      }

      if ( !selectedFavoriteItem.Show.IsCollected() && TraktHelper.IsMpNZBAvailableAndEnabled )
      {
        // Search for show with mpNZB
        listItem = new GUIListItem( Translation.SearchWithMpNZB );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
      }

      if ( !selectedFavoriteItem.Show.IsCollected() && TraktHelper.IsMyTorrentsAvailableAndEnabled )
      {
        // Search for show with MyTorrents
        listItem = new GUIListItem( Translation.SearchTorrent );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.SearchTorrent;
      }

      // Show Context Menu
      dlg.DoModal( GUIWindowManager.ActiveWindow );
      if ( dlg.SelectedId < 0 )
        return;

      switch ( dlg.SelectedId )
      {
        case ( (int)ContextMenuItem.ShowSeasonInfo ):
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.ShowSeasons, selectedFavoriteItem.Show.ToJSON() );
          break;

        case ( (int)ContextMenuItem.AddToWatchList ):
          TraktHelper.AddShowToWatchList( selectedFavoriteItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.AddToFavorites ):
          // could be adding to favourites from your friends favourite list
          TraktHelper.AddShowToFavorites( selectedFavoriteItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.RemoveFromFavorites ):
          PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
          TraktHelper.RemoveShowFromFavorites( selectedFavoriteItem.Show );
          if ( _FavoriteShows.Count() >= 1 )
          {
            // remove from list
            var showsToExcept = new List<TraktFavoriteItem> { selectedFavoriteItem };
            _FavoriteShows = FavoriteShows?.Except( showsToExcept );
            userFavorites[ CurrentUser ] = _FavoriteShows;
            LoadFavoriteShows();
          }
          else
          {
            // no more shows left
            ClearProperties();
            GUIControl.ClearControl( GetID, Facade.GetID );
            _FavoriteShows = null;
            userFavorites.Remove( CurrentUser );
            // notify and exit
            GUIUtils.ShowNotifyDialog( GUIUtils.PluginName(), Translation.NoShowFavorites );
            GUIWindowManager.ShowPreviousWindow();
            return;
          }
          break;

        case ( (int)ContextMenuItem.RemoveFromWatchList ):
          PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
          TraktHelper.RemoveShowFromWatchList( selectedFavoriteItem.Show );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.AddToList ):
          TraktHelper.AddRemoveShowInUserList( selectedFavoriteItem.Show, false );
          break;

        case ( (int)ContextMenuItem.Trailers ):
          GUICommon.ShowTVShowTrailersMenu( selectedFavoriteItem.Show );
          break;

        case ( (int)ContextMenuItem.Related ):
          TraktHelper.ShowRelatedShows( selectedFavoriteItem.Show );
          break;

        case ( (int)ContextMenuItem.Rate ):
          GUICommon.RateShow( selectedFavoriteItem.Show );
          OnShowSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIShowListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( CurrentUser != TraktSettings.Username )
            GUIWatchListShows.ClearCache( TraktSettings.Username );
          break;

        case ( (int)ContextMenuItem.Shouts ):
          TraktHelper.ShowTVShowShouts( selectedFavoriteItem.Show );
          break;

        case ( (int)ContextMenuItem.Cast ):
          GUICreditsShow.Show = selectedFavoriteItem.Show;
          GUICreditsShow.Type = GUICreditsShow.CreditType.Cast;
          GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename( selectedItem.Images.ShowImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsShow );
          break;

        case ( (int)ContextMenuItem.Crew ):
          GUICreditsShow.Show = selectedFavoriteItem.Show;
          GUICreditsShow.Type = GUICreditsShow.CreditType.Crew;
          GUICreditsShow.Fanart = TmdbCache.GetShowBackdropFilename( selectedItem.Images.ShowImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsShow );
          break;

        case ( (int)ContextMenuItem.ChangeLayout ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        case ( (int)ContextMenuItem.SearchWithMpNZB ):
          string loadingParam = string.Format( "search:{0}", selectedFavoriteItem.Show.Title );
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MpNZB, loadingParam );
          break;

        case ( (int)ContextMenuItem.SearchTorrent ):
          string loadPar = selectedFavoriteItem.Show.Title;
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MyTorrents, loadPar );
          break;

        default:
          break;
      }

      base.OnShowContextMenu();
    }

    #endregion

    #region Private Methods

    private void CheckAndPlayEpisode( bool jumpTo )
    {
      var selectedItem = this.Facade.SelectedListItem;
      if ( selectedItem == null )
        return;

      var selecteItem = selectedItem.TVTag as TraktFavoriteItem;
      GUICommon.CheckAndPlayFirstUnwatchedEpisode( selecteItem.Show, jumpTo );
    }

    private void LoadFavoriteShows()
    {
      GUIUtils.SetProperty( "#Trakt.Items", string.Empty );

      GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback( () =>
      {
        return FavoriteShows;
      },
      delegate ( bool success, object result )
      {
        if ( success )
        {
          var favorites = result as IEnumerable<TraktFavoriteItem>;
          SendFavoriteShowsToFacade( favorites );
        }
      }, Translation.GettingFavorites, true );
    }

    private void SendFavoriteShowsToFacade( IEnumerable<TraktFavoriteItem> showFavorites )
    {
      // clear facade
      GUIControl.ClearControl( GetID, Facade.GetID );

      if ( showFavorites == null )
      {
        GUIUtils.ShowNotifyDialog( Translation.Error, Translation.ErrorGeneral );
        GUIWindowManager.ShowPreviousWindow();
        return;
      }

      if ( showFavorites.Count() == 0 )
      {
        GUIUtils.ShowNotifyDialog( GUIUtils.PluginName(), string.Format( Translation.NoShowFavorites, CurrentUser ) );
        CurrentUser = TraktSettings.Username;
        GUIWindowManager.ShowPreviousWindow();
        return;
      }

      // sort shows
      var sortedList = showFavorites.Where( s => !string.IsNullOrEmpty( s.Show.Title ) ).ToList();
      sortedList.Sort( new GUIListItemShowSorter( TraktSettings.SortByUserFavoriteShows.Field, TraktSettings.SortByUserFavoriteShows.Direction ) );

      int itemId = 0;
      var showImages = new List<GUITmdbImage>();

      // Add each show
      foreach ( var favoriteItem in sortedList )
      {
        // add image for download
        var images = new GUITmdbImage { ShowImages = new TmdbShowImages { Id = favoriteItem.Show.Ids.Tmdb } };
        showImages.Add( images );

        var item = new GUIShowListItem( favoriteItem.Show.Title, (int)TraktGUIWindows.UserFavoriteShows );

        item.Label2 = favoriteItem.Show.Year == null ? "----" : favoriteItem.Show.Year.ToString();
        item.TVTag = favoriteItem;
        item.Show = favoriteItem.Show;
        item.Images = images;
        item.ItemId = Int32.MaxValue - itemId;
        item.IsPlayed = favoriteItem.Show.IsWatched();
        item.IconImage = GUIImageHandler.GetDefaultPoster( false );
        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
        item.OnItemSelected += OnShowSelected;
        Utils.SetDefaultIcons( item );
        Facade.Add( item );
        itemId++;
      }

      // Set Facade Layout
      Facade.CurrentLayout = CurrentLayout;
      GUIControl.FocusControl( GetID, Facade.GetID );

      if ( PreviousSelectedIndex >= showFavorites.Count() )
        Facade.SelectIndex( PreviousSelectedIndex - 1 );
      else
        Facade.SelectIndex( PreviousSelectedIndex );

      // set facade properties
      GUIUtils.SetProperty( "#itemcount", showFavorites.Count().ToString() );
      GUIUtils.SetProperty( "#Trakt.Items", string.Format( "{0} {1}", showFavorites.Count().ToString(), showFavorites.Count() > 1 ? Translation.Shows : Translation.Show ) );

      // Download show images Async and set to facade
      GUIShowListItem.GetImages( showImages );
    }

    private void InitProperties()
    {
      // Fanart
      backdrop.GUIImageOne = FanartBackground;
      backdrop.GUIImageTwo = FanartBackground2;
      backdrop.LoadingImage = loadingImage;

      // load Favorite shows for user
      if ( string.IsNullOrEmpty( CurrentUser ) )
        CurrentUser = TraktSettings.Username;
      GUICommon.SetProperty( "#Trakt.FavoriteShows.CurrentUser", CurrentUser );

      // load last layout
      CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.UserFavoriteShowsDefaultLayout;

      // Update Button States
      UpdateButtonState();

      if ( sortButton != null )
      {
        sortButton.SortChanged += ( o, e ) =>
        {
          TraktSettings.SortByUserFavoriteShows.Direction = (SortingDirections)( e.Order - 1 );
          PreviousSelectedIndex = 0;
          UpdateButtonState();
          LoadFavoriteShows();
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
        sortButton.Label = GUICommon.GetSortByString( TraktSettings.SortByUserFavoriteShows );
        sortButton.IsAscending = ( TraktSettings.SortByUserFavoriteShows.Direction == SortingDirections.Ascending );
      }
      GUIUtils.SetProperty( "#Trakt.SortBy", GUICommon.GetSortByString( TraktSettings.SortByUserFavoriteShows ) );
    }

    private void ClearProperties()
    {
      GUIUtils.SetProperty( "#Trakt.Show.Favorite.Inserted", string.Empty );
      GUIUtils.SetProperty( "#Trakt.Show.Favorite.Notes", string.Empty );
      GUICommon.ClearShowProperties();
    }

    private void PublishFavoriteSkinProperties( TraktFavoriteItem item )
    {
      GUICommon.SetProperty( "#Trakt.Show.Favorite.Inserted", item.ListedAt.FromISO8601().ToShortDateString() );
      GUICommon.SetProperty( "#Trakt.Show.Favorite.Notes", item.Notes );
      GUICommon.SetShowProperties( item.Show );
    }

    private void OnShowSelected( GUIListItem item, GUIControl parent )
    {
      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      var favoriteItem = item.TVTag as TraktFavoriteItem;
      PublishFavoriteSkinProperties( favoriteItem );

      string fanart = TmdbCache.GetShowBackdropFilename( ( item as GUIShowListItem ).Images.ShowImages );
      if ( !string.IsNullOrEmpty( fanart ) )
      {
        GUIImageHandler.LoadFanart( backdrop, fanart );
      }
    }
    #endregion

    #region Public Methods

    public static void ClearCache( string username )
    {
      if ( userFavorites.Keys.Contains( username ) )
        userFavorites.Remove( username );
    }

    #endregion
  }
}