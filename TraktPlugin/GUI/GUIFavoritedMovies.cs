using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.GUI.Library;
using MediaPortal.Util;
using TraktPlugin.Cache;
using TraktPlugin.TmdbAPI.DataModels;
using TraktAPI.DataModels;
using Action = MediaPortal.GUI.Library.Action;

namespace TraktPlugin.GUI
{
  public class GUIFavoritedMovies : GUIWindow
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

    public GUIFavoritedMovies()
    {
      backdrop = new ImageSwapper
      {
        PropertyOne = "#Trakt.FavoritedMovies.Fanart.1",
        PropertyTwo = "#Trakt.FavoritedMovies.Fanart.2"
      };
    }

    #endregion

    #region Private Variables

    private Dictionary<int, TraktMoviesFavorited> FavoritedMoviePages = null;
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
        return (int)TraktGUIWindows.FavoritedMovies;
      }
    }

    public override bool Init()
    {
      return Load( GUIGraphicsContext.Skin + @"\Trakt.Favorited.Movies.xml" );
    }

    protected override void OnPageLoad()
    {
      base.OnPageLoad();

      // Clear GUI Properties
      ClearProperties();

      // Init Properties
      InitProperties();

      // Load Favorited Movies
      LoadFavoritedMovies( CurrentPage );
    }

    protected override void OnPageDestroy( int new_windowId )
    {
      GUIMovieListItem.StopDownload = true;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;
      ClearProperties();

      // save current layout
      TraktSettings.FavoritedMoviesDefaultLayout = (int)CurrentLayout;

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
            var item = Facade.SelectedListItem as GUIMovieListItem;
            if ( item == null )
              return;

            if ( !item.IsFolder )
            {
              CheckAndPlayMovie( true );
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
              LoadFavoritedMovies( CurrentPage );
            }
          }
          break;

        // Layout Button
        case ( 2 ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        // Sort Button
        case ( 8 ):
          var newSortBy = GUICommon.ShowSortMenu( TraktSettings.SortByFavoritedMovies );
          if ( newSortBy != null )
          {
            if ( newSortBy.Field != TraktSettings.SortByFavoritedMovies.Field )
            {
              TraktSettings.SortByFavoritedMovies = newSortBy;
              PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
              UpdateButtonState();
              LoadFavoritedMovies( CurrentPage );
            }
          }
          break;

        // Hide Watched
        case ( 9 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedMoviesHideWatched = !TraktSettings.FavoritedMoviesHideWatched;
          UpdateButtonState();
          LoadFavoritedMovies( CurrentPage );
          break;

        // Hide Watchlisted
        case ( 10 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedMoviesHideWatchlisted = !TraktSettings.FavoritedMoviesHideWatchlisted;
          UpdateButtonState();
          LoadFavoritedMovies( CurrentPage );
          break;

        // Hide Collected
        case ( 11 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedMoviesHideCollected = !TraktSettings.FavoritedMoviesHideCollected;
          UpdateButtonState();
          LoadFavoritedMovies( CurrentPage );
          break;

        // Hide Rated
        case ( 12 ):
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          TraktSettings.FavoritedMoviesHideRated = !TraktSettings.FavoritedMoviesHideRated;
          UpdateButtonState();
          LoadFavoritedMovies( CurrentPage );
          break;

        // Time Period Button
        case ( 13 ):
          var newPeriod = GUICommon.ShowFavoritedPeriodMenu( TraktSettings.FavoritedMoviesPeriod );
          if ( newPeriod != TraktSettings.FavoritedMoviesPeriod )
          {
            TraktSettings.FavoritedMoviesPeriod = newPeriod;
            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
            FavoritedMoviePages = null;
            UpdateButtonState();
            LoadFavoritedMovies( CurrentPage );
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
          CheckAndPlayMovie( false );
          break;
        default:
          base.OnAction( action );
          break;
      }
    }

    protected override void OnShowContextMenu()
    {
      var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
      if ( selectedItem == null )
        return;

      var selectedFavoritedItem = selectedItem.TVTag as TraktMovieFavorited;
      if ( selectedFavoritedItem == null )
        return;

      var dlg = (IDialogbox)GUIWindowManager.GetWindow( (int)GUIWindow.Window.WINDOW_DIALOG_MENU );
      if ( dlg == null )
        return;

      dlg.Reset();
      dlg.SetHeading( GUIUtils.PluginName() );

      GUICommon.CreateMoviesContextMenu( ref dlg, selectedFavoritedItem.Movie, false );

      // Show Context Menu
      dlg.DoModal( GUIWindowManager.ActiveWindow );
      if ( dlg.SelectedId < 0 )
        return;

      switch ( dlg.SelectedId )
      {
        case ( (int)MediaContextMenuItem.MarkAsWatched ):
          TraktHelper.AddMovieToWatchHistory( selectedFavoritedItem.Movie );
          selectedItem.IsPlayed = true;
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedMoviesHideWatched )
            LoadFavoritedMovies( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.MarkAsUnWatched ):
          TraktHelper.RemoveMovieFromWatchHistory( selectedFavoritedItem.Movie );
          selectedItem.IsPlayed = false;
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.AddToWatchList ):
          TraktHelper.AddMovieToWatchList( selectedFavoritedItem.Movie, true );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedMoviesHideWatchlisted )
            LoadFavoritedMovies( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.RemoveFromWatchList ):
          TraktHelper.RemoveMovieFromWatchList( selectedFavoritedItem.Movie, true );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.AddToFavorites ):
          TraktHelper.AddMovieToFavorites( selectedFavoritedItem.Movie, true );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.RemoveFromFavorites ):
          TraktHelper.RemoveMovieFromFavorites( selectedFavoritedItem.Movie, true );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.AddToList ):
          TraktHelper.AddRemoveMovieInUserList( selectedFavoritedItem.Movie, false );
          break;

        case ( (int)MediaContextMenuItem.AddToLibrary ):
          TraktHelper.AddMovieToCollection( selectedFavoritedItem.Movie );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedMoviesHideCollected )
            LoadFavoritedMovies( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.RemoveFromLibrary ):
          TraktHelper.RemoveMovieFromCollection( selectedFavoritedItem.Movie );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)MediaContextMenuItem.Related ):
          TraktHelper.ShowRelatedMovies( selectedFavoritedItem.Movie );
          break;

        case ( (int)MediaContextMenuItem.Rate ):
          GUICommon.RateMovie( selectedFavoritedItem.Movie );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( TraktSettings.FavoritedMoviesHideRated )
            LoadFavoritedMovies( CurrentPage );
          break;

        case ( (int)MediaContextMenuItem.Filters ):
          if ( GUICommon.ShowMovieFiltersMenu() )
          {
            PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
            UpdateButtonState();
            LoadFavoritedMovies( CurrentPage );
          }
          break;

        case ( (int)MediaContextMenuItem.Shouts ):
          TraktHelper.ShowMovieShouts( selectedFavoritedItem.Movie );
          break;

        case ( (int)MediaContextMenuItem.Cast ):
          GUICreditsMovie.Movie = selectedFavoritedItem.Movie;
          GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
          GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename( selectedItem.Images.MovieImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsMovie );
          break;

        case ( (int)MediaContextMenuItem.Crew ):
          GUICreditsMovie.Movie = selectedFavoritedItem.Movie;
          GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
          GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename( selectedItem.Images.MovieImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsMovie );
          break;

        case ( (int)MediaContextMenuItem.Trailers ):
          GUICommon.ShowMovieTrailersMenu( selectedFavoritedItem.Movie );
          break;

        case ( (int)MediaContextMenuItem.ChangeLayout ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        case ( (int)MediaContextMenuItem.SearchWithMpNZB ):
          string loadingParam = string.Format( "search:{0}", selectedFavoritedItem.Movie.Title );
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MpNZB, loadingParam );
          break;

        case ( (int)MediaContextMenuItem.SearchTorrent ):
          string loadPar = selectedFavoritedItem.Movie.Title;
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MyTorrents, loadPar );
          break;

        default:
          break;
      }

      base.OnShowContextMenu();
    }

    #endregion

    #region Private Methods

    TraktMoviesFavorited GetFavoritedMovies( int page )
    {
      TraktMoviesFavorited favoritedMovies;

      if ( FavoritedMoviePages == null || LastRequest < DateTime.UtcNow.Subtract( new TimeSpan( 0, TraktSettings.WebRequestCacheMinutes, 0 ) ) )
      {
        // get the first page
        favoritedMovies = TraktAPI.TraktAPI.GetFavoritedMovies( period: TraktSettings.FavoritedMoviesPeriod, page: 1, maxItems: TraktSettings.MaxFavoritedMoviesRequest );

        // reset to defaults
        LastRequest = DateTime.UtcNow;
        CurrentPage = 1;
        PreviousSelectedIndex = 0;

        // clear the cache
        if ( FavoritedMoviePages == null )
          FavoritedMoviePages = new Dictionary<int, TraktMoviesFavorited>();
        else
          FavoritedMoviePages.Clear();

        // add page to cache
        FavoritedMoviePages.Add( 1, favoritedMovies );
      }
      else
      {
        // get page from cache if it exists
        if ( FavoritedMoviePages.TryGetValue( page, out favoritedMovies ) )
        {
          return favoritedMovies;
        }

        // request next page
        favoritedMovies = TraktAPI.TraktAPI.GetFavoritedMovies( period: TraktSettings.FavoritedMoviesPeriod, page: page, maxItems: TraktSettings.MaxFavoritedMoviesRequest );
        if ( favoritedMovies != null && favoritedMovies.Movies != null )
        {
          // add to cache
          FavoritedMoviePages.Add( page, favoritedMovies );
        }
      }
      return favoritedMovies;
    }

    private void CheckAndPlayMovie( bool jumpTo )
    {
      var selectedItem = this.Facade.SelectedListItem;
      if ( selectedItem == null )
        return;

      var selectedFavoritedItem = selectedItem.TVTag as TraktMovieFavorited;
      if ( selectedFavoritedItem == null )
        return;

      GUICommon.CheckAndPlayMovie( jumpTo, selectedFavoritedItem.Movie );
    }

    private void LoadFavoritedMovies( int page = 1 )
    {
      GUIUtils.SetProperty( "#Trakt.Items", string.Empty );

      GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback( () =>
      {
        return GetFavoritedMovies( page );
      },
      delegate ( bool success, object result )
      {
        if ( success )
        {
          var movies = result as TraktMoviesFavorited;
          SendFavoritedMoviesToFacade( movies );
        }
      }, Translation.GettingFavoritedMovies, true );
    }

    private void SendFavoritedMoviesToFacade( TraktMoviesFavorited favoritedItems )
    {
      // clear facade
      GUIControl.ClearControl( GetID, Facade.GetID );

      if ( favoritedItems == null )
      {
        GUIUtils.ShowNotifyDialog( Translation.Error, Translation.ErrorGeneral );
        GUIWindowManager.ShowPreviousWindow();
        FavoritedMoviePages = null;
        return;
      }

      if ( favoritedItems.Movies.Count() == 0 )
      {
        GUIUtils.ShowNotifyDialog( GUIUtils.PluginName(), Translation.NoFavoritedMovies );
        GUIWindowManager.ShowPreviousWindow();
        FavoritedMoviePages = null;
        return;
      }

      // filter movies
      var filteredFavoritedList = GUICommon.FilterFavoritedMovies( favoritedItems.Movies ).Where( m => !string.IsNullOrEmpty( m.Movie.Title ) ).ToList();

      // sort movies
      filteredFavoritedList.Sort( new GUIListItemMovieSorter( TraktSettings.SortByFavoritedMovies.Field, TraktSettings.SortByFavoritedMovies.Direction ) );

      int itemId = 0;
      var movieImages = new List<GUITmdbImage>();

      // Add Previous Page Button
      if ( favoritedItems.CurrentPage != 1 )
      {
        var prevPageItem = new GUIMovieListItem( Translation.PreviousPage, (int)TraktGUIWindows.FavoritedMovies )
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

      // Add each movie mark remote if not in collection            
      foreach ( var favoritedItem in filteredFavoritedList )
      {
        // add image for download
        var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = favoritedItem.Movie.Ids.Tmdb } };
        movieImages.Add( images );

        var item = new GUIMovieListItem( favoritedItem.Movie.Title, (int)TraktGUIWindows.FavoritedMovies )
        {
          Label2 = favoritedItem.Movie.Year == null ? "----" : favoritedItem.Movie.Year.ToString(),
          TVTag = favoritedItem,
          Movie = favoritedItem.Movie,
          Images = images,
          IsPlayed = favoritedItem.Movie.IsWatched(),
          ItemId = Int32.MaxValue - itemId,
          IconImage = GUIImageHandler.GetDefaultPoster( false ),
          IconImageBig = GUIImageHandler.GetDefaultPoster(),
          ThumbnailImage = GUIImageHandler.GetDefaultPoster()
        };

        item.OnItemSelected += OnMovieSelected;
        Utils.SetDefaultIcons( item );
        Facade.Add( item );
        itemId++;
      }

      // Add Next Page Button
      if ( favoritedItems.CurrentPage != favoritedItems.TotalPages )
      {
        var nextPageItem = new GUIMovieListItem( Translation.NextPage, (int)TraktGUIWindows.FavoritedMovies );
        nextPageItem.IsNextPageItem = true;
        nextPageItem.IconImage = "traktNextPage.png";
        nextPageItem.IconImageBig = "traktNextPage.png";
        nextPageItem.ThumbnailImage = "traktNextPage.png";
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
      GUIUtils.SetProperty( "#Trakt.Items", string.Format( "{0} {1}", filteredFavoritedList.Count(), filteredFavoritedList.Count() > 1 ? Translation.Movies : Translation.Movie ) );

      // Page Properties
      GUIUtils.SetProperty( "#Trakt.Facade.CurrentPage", favoritedItems.CurrentPage.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.TotalPages", favoritedItems.TotalPages.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.TotalItemsPerPage", TraktSettings.MaxFavoritedMoviesRequest.ToString() );

      // Download movie images Async and set to facade
      GUIMovieListItem.GetImages( movieImages );
    }

    private void InitProperties()
    {
      // Fanart
      backdrop.GUIImageOne = FanartBackground;
      backdrop.GUIImageTwo = FanartBackground2;
      backdrop.LoadingImage = loadingImage;

      // load last layout
      CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.FavoritedMoviesDefaultLayout;

      // Update Button States
      UpdateButtonState();

      if ( sortButton != null )
      {
        UpdateButtonState();
        sortButton.SortChanged += ( o, e ) =>
        {
          TraktSettings.SortByFavoritedMovies.Direction = (SortingDirections)( e.Order - 1 );
          PreviousSelectedIndex = CurrentPage == 1 ? 0 : 1;
          UpdateButtonState();
          LoadFavoritedMovies( CurrentPage );
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
        sortButton.Label = GUICommon.GetSortByString( TraktSettings.SortByFavoritedMovies );
        sortButton.IsAscending = ( TraktSettings.SortByFavoritedMovies.Direction == SortingDirections.Ascending );
      }

      if ( periodButton != null )
      {
        periodButton.Label = GUICommon.GetPeriodString( TraktSettings.FavoritedMoviesPeriod );
      }

      GUIUtils.SetProperty( "#Trakt.FavoritedMovies.Period", GUICommon.GetTranslatedFavoritedPeriod( TraktSettings.FavoritedMoviesPeriod ) );
      GUIUtils.SetProperty( "#Trakt.SortBy", GUICommon.GetSortByString( TraktSettings.SortByFavoritedMovies ) );

      // update filter buttons
      if ( filterWatchedButton != null )
        filterWatchedButton.Selected = TraktSettings.FavoritedMoviesHideWatched;
      if ( filterWatchListedButton != null )
        filterWatchListedButton.Selected = TraktSettings.FavoritedMoviesHideWatchlisted;
      if ( filterCollectedButton != null )
        filterCollectedButton.Selected = TraktSettings.FavoritedMoviesHideCollected;
      if ( filterRatedButton != null )
        filterRatedButton.Selected = TraktSettings.FavoritedMoviesHideRated;
    }

    private void ClearProperties( bool moviesOnly = false )
    {
      if ( !moviesOnly )
      {
        GUIUtils.SetProperty( "#Trakt.FavoritedMovies.Period", string.Empty );
        GUIUtils.SetProperty( "#Trakt.FavoritedMovies.CurrentPage", string.Empty );
        GUIUtils.SetProperty( "#Trakt.FavoritedMovies.TotalPages", string.Empty );
        GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", string.Empty );
      }

      GUIUtils.SetProperty( "#Trakt.Movie.UserCount", string.Empty );

      GUICommon.ClearMovieProperties();
    }

    private void PublishMovieSkinProperties( TraktMovieFavorited favoritedItem )
    {
      GUICommon.SetProperty( "#Trakt.Movie.UserCount", favoritedItem.UserCount.ToString() );

      GUICommon.SetMovieProperties( favoritedItem.Movie );
    }

    private void OnMovieSelected( GUIListItem item, GUIControl control )
    {
      GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", false.ToString() );

      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      var favoritedItem = item.TVTag as TraktMovieFavorited;
      if ( favoritedItem == null )
        return;

      PublishMovieSkinProperties( favoritedItem );
      GUIImageHandler.LoadFanart( backdrop, TmdbCache.GetMovieBackdropFilename( ( item as GUIMovieListItem ).Images.MovieImages ) );
    }

    private void OnNextPageSelected( GUIListItem item, GUIControl control )
    {
      GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", true.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.PageToLoad", ( CurrentPage + 1 ).ToString() );

      backdrop.Filename = string.Empty;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      // only clear the last selected movie properties
      ClearProperties( true );
    }

    private void OnPreviousPageSelected( GUIListItem item, GUIControl control )
    {
      GUIUtils.SetProperty( "#Trakt.Facade.IsPageItem", true.ToString() );
      GUIUtils.SetProperty( "#Trakt.Facade.PageToLoad", ( CurrentPage - 1 ).ToString() );

      backdrop.Filename = string.Empty;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      // only clear the last selected movie properties
      ClearProperties( true );
    }

    #endregion
  }
}
