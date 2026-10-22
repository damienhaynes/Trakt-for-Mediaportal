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
  public class GUIUserFavoriteMovies : GUIWindow
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
      RemoveFromFavorites,
      AddToFavorites,
      RemoveFromWatchList,
      AddToWatchList,
      AddToList,
      ChangeLayout,
      MarkAsWatched,
      MarkAsUnWatched,
      AddToLibrary,
      RemoveFromLibrary,
      Related,
      Rate,
      Shouts,
      Cast,
      Crew,
      Trailers,
      SearchWithMpNZB,
      SearchTorrent
    }

    #endregion

    #region Constructor

    public GUIUserFavoriteMovies()
    {
      backdrop = new ImageSwapper
      {
        PropertyOne = "#Trakt.UserFavoriteMovies.Fanart.1",
        PropertyTwo = "#Trakt.UserFavoriteMovies.Fanart.2"
      };
    }

    #endregion

    #region Private Variables

    private GUIFacadeControl.Layout CurrentLayout { get; set; }
    static int PreviousSelectedIndex { get; set; }
    private readonly ImageSwapper backdrop;
    static DateTime LastRequest = new DateTime();
    static readonly Dictionary<string, IEnumerable<TraktFavoriteItem>> userFavorites = new Dictionary<string, IEnumerable<TraktFavoriteItem>>();

    static IEnumerable<TraktFavoriteItem> FavoriteMovies
    {
      get
      {
        if ( !userFavorites.Keys.Contains( CurrentUser ) || LastRequest < DateTime.UtcNow.Subtract( new TimeSpan( 0, TraktSettings.WebRequestCacheMinutes, 0 ) ) )
        {
          string username = CurrentUser == TraktSettings.Username ? "me" : CurrentUser;

          // NB: since we're returning all items there is no need to use the sortby API parameters for each page request
          int maxItemsPerPage = 100;
          TraktFavoriteItems favoriteItems = TraktAPI.TraktAPI.GetFavourites( username, type: "movies", extendedInfoParams: "full", page: 1, maxItems: maxItemsPerPage );

          if ( favoriteItems == null || favoriteItems.Items == null )
          {
            userFavorites.Remove( CurrentUser );
            return null;
          }

          _FavoriteMovies = favoriteItems.Items;

          // get next page(s) if required
          while ( favoriteItems.CurrentPage < favoriteItems.TotalPages )
          {
            // Note: API returns total pages for all watchlist types not just this one (movies)
            // so we need to check returned items against our expected max items per page
            if ( _FavoriteMovies.Count() < ( maxItemsPerPage * favoriteItems.CurrentPage ) )
              break;

            favoriteItems = TraktAPI.TraktAPI.GetFavourites( username, type: "movies", extendedInfoParams: "full", page: favoriteItems.CurrentPage + 1, maxItems: maxItemsPerPage );
            if ( favoriteItems == null || favoriteItems.Items == null )
              break;

            _FavoriteMovies = _FavoriteMovies.Concat( favoriteItems.Items );
          }

          if ( userFavorites.Keys.Contains( CurrentUser ) )
            userFavorites.Remove( CurrentUser );

          userFavorites.Add( CurrentUser, _FavoriteMovies );
          LastRequest = DateTime.UtcNow;
          PreviousSelectedIndex = 0;
        }

        return userFavorites[ CurrentUser ];
      }
    }
    static IEnumerable<TraktFavoriteItem> _FavoriteMovies = null;

    #endregion

    #region Public Properties

    public static string CurrentUser { get; set; }

    #endregion

    #region Base Overrides

    public override int GetID
    {
      get
      {
        return (int)TraktGUIWindows.UserFavoriteMovies;
      }
    }

    public override bool Init()
    {
      return Load( GUIGraphicsContext.Skin + @"\Trakt.UserFavorite.Movies.xml" );
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

      // Load Favorite Movies
      LoadFavoriteMovies();
    }

    protected override void OnPageDestroy( int new_windowId )
    {
      GUIMovieListItem.StopDownload = true;
      PreviousSelectedIndex = Facade.SelectedListItemIndex;
      ClearProperties();

      // save current layout
      TraktSettings.UserFavoriteMoviesDefaultLayout = (int)CurrentLayout;

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
            CheckAndPlayMovie( true );
          }
          break;

        // Layout Button
        case ( 2 ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        // Sort Button
        case ( 8 ):
          var newSortBy = GUICommon.ShowSortMenu( TraktSettings.SortByUserFavoriteMovies );
          if ( newSortBy != null )
          {
            if ( newSortBy.Field != TraktSettings.SortByUserFavoriteMovies.Field )
            {
              TraktSettings.SortByUserFavoriteMovies = newSortBy;
              PreviousSelectedIndex = 0;
              UpdateButtonState();
              LoadFavoriteMovies();
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

      var selectedFavoriteItem = selectedItem.TVTag as TraktFavoriteItem;
      if ( selectedFavoriteItem == null )
        return;

      var dlg = (IDialogbox)GUIWindowManager.GetWindow( (int)GUIWindow.Window.WINDOW_DIALOG_MENU );
      if ( dlg == null )
        return;

      dlg.Reset();
      dlg.SetHeading( GUIUtils.PluginName() );

      GUIListItem listItem = null;

      // only allow removal if viewing your own favorites
      if ( CurrentUser == TraktSettings.Username )
      {
        listItem = new GUIListItem( Translation.RemoveFromFavorites );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.RemoveFromFavorites;
      }
      else if ( !selectedFavoriteItem.Movie.IsFavorited() )
      {
        // viewing someone else's favorites and not in yours
        listItem = new GUIListItem( Translation.AddToFavorites );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.AddToFavorites;
      }

      // Add to Watchlist
      if ( !selectedFavoriteItem.Movie.IsWatchlisted() )
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

      // Mark As Watched
      if ( !selectedFavoriteItem.Movie.IsWatched() )
      {
        listItem = new GUIListItem( Translation.MarkAsWatched );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.MarkAsWatched;
      }

      // Mark As UnWatched
      if ( selectedFavoriteItem.Movie.IsWatched() )
      {
        listItem = new GUIListItem( Translation.MarkAsUnWatched );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.MarkAsUnWatched;
      }

      // Add to Library
      // Don't allow if it will be removed again on next sync
      // movie could be part of a DVD collection
      if ( !selectedFavoriteItem.Movie.IsCollected() && !TraktSettings.KeepTraktLibraryClean )
      {
        listItem = new GUIListItem( Translation.AddToLibrary );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.AddToLibrary;
      }

      if ( selectedFavoriteItem.Movie.IsCollected() )
      {
        listItem = new GUIListItem( Translation.RemoveFromLibrary );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.RemoveFromLibrary;
      }

      // Related Movies
      listItem = new GUIListItem( Translation.RelatedMovies );
      dlg.Add( listItem );
      listItem.ItemId = (int)ContextMenuItem.Related;

      // Rate Movie
      listItem = new GUIListItem( Translation.RateMovie );
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

      if ( !selectedFavoriteItem.Movie.IsCollected() && TraktHelper.IsMpNZBAvailableAndEnabled )
      {
        // Search for movie with mpNZB
        listItem = new GUIListItem( Translation.SearchWithMpNZB );
        dlg.Add( listItem );
        listItem.ItemId = (int)ContextMenuItem.SearchWithMpNZB;
      }

      if ( !selectedFavoriteItem.Movie.IsCollected() && TraktHelper.IsMyTorrentsAvailableAndEnabled )
      {
        // Search for movie with MyTorrents
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
        case ( (int)ContextMenuItem.MarkAsWatched ):
          TraktHelper.AddMovieToWatchHistory( selectedFavoriteItem.Movie );
          selectedItem.IsPlayed = true;
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          GUIWatchListMovies.ClearCache( TraktSettings.Username );
          break;

        case ( (int)ContextMenuItem.MarkAsUnWatched ):
          TraktHelper.RemoveMovieFromWatchHistory( selectedFavoriteItem.Movie );
          selectedItem.IsPlayed = false;
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.AddToWatchList ):
          TraktHelper.AddMovieToWatchList( selectedFavoriteItem.Movie, true );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.AddToFavorites ):
          // could be adding to favourites from your friends favourite list
          TraktHelper.AddMovieToFavorites( selectedFavoriteItem.Movie, true );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.RemoveFromFavorites ):
          PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
          TraktHelper.RemoveMovieFromFavorites( selectedFavoriteItem.Movie, true );
          if ( _FavoriteMovies.Count() >= 1 )
          {
            // remove from list
            var moviesToExcept = new List<TraktFavoriteItem> { selectedFavoriteItem };
            _FavoriteMovies = FavoriteMovies?.Except( moviesToExcept );
            userFavorites[ CurrentUser ] = _FavoriteMovies;
            LoadFavoriteMovies();
          }
          else
          {
            // no more movies left
            ClearProperties();
            GUIControl.ClearControl( GetID, Facade.GetID );
            _FavoriteMovies = null;
            userFavorites.Remove( CurrentUser );
            // notify and exit
            GUIUtils.ShowNotifyDialog( GUIUtils.PluginName(), Translation.NoMovieFavorites );
            GUIWindowManager.ShowPreviousWindow();
            return;
          }
          break;

        case ( (int)ContextMenuItem.RemoveFromWatchList ):
          PreviousSelectedIndex = this.Facade.SelectedListItemIndex;
          TraktHelper.RemoveMovieFromWatchList( selectedFavoriteItem.Movie, true );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          break;

        case ( (int)ContextMenuItem.AddToList ):
          TraktHelper.AddRemoveMovieInUserList( selectedFavoriteItem.Movie, false );
          break;

        case ( (int)ContextMenuItem.Trailers ):
          GUICommon.ShowMovieTrailersMenu( selectedFavoriteItem.Movie );
          break;

        case ( (int)ContextMenuItem.AddToLibrary ):
          TraktHelper.AddMovieToCollection( selectedFavoriteItem.Movie );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( CurrentUser != TraktSettings.Username )
            GUIWatchListMovies.ClearCache( TraktSettings.Username );
          break;

        case ( (int)ContextMenuItem.RemoveFromLibrary ):
          TraktHelper.RemoveMovieFromCollection( selectedFavoriteItem.Movie );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( CurrentUser != TraktSettings.Username )
            GUIWatchListMovies.ClearCache( TraktSettings.Username );
          break;

        case ( (int)ContextMenuItem.Related ):
          TraktHelper.ShowRelatedMovies( selectedFavoriteItem.Movie );
          break;

        case ( (int)ContextMenuItem.Rate ):
          GUICommon.RateMovie( selectedFavoriteItem.Movie );
          OnMovieSelected( selectedItem, Facade );
          ( Facade.SelectedListItem as GUIMovieListItem ).Images.NotifyPropertyChanged( "Poster" );
          if ( CurrentUser != TraktSettings.Username )
            GUIWatchListMovies.ClearCache( TraktSettings.Username );
          break;

        case ( (int)ContextMenuItem.Shouts ):
          TraktHelper.ShowMovieShouts( selectedFavoriteItem.Movie );
          break;

        case ( (int)ContextMenuItem.Cast ):
          GUICreditsMovie.Movie = selectedFavoriteItem.Movie;
          GUICreditsMovie.Type = GUICreditsMovie.CreditType.Cast;
          GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename( selectedItem.Images.MovieImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsMovie );
          break;

        case ( (int)ContextMenuItem.Crew ):
          GUICreditsMovie.Movie = selectedFavoriteItem.Movie;
          GUICreditsMovie.Type = GUICreditsMovie.CreditType.Crew;
          GUICreditsMovie.Fanart = TmdbCache.GetMovieBackdropFilename( selectedItem.Images.MovieImages );
          GUIWindowManager.ActivateWindow( (int)TraktGUIWindows.CreditsMovie );
          break;

        case ( (int)ContextMenuItem.ChangeLayout ):
          CurrentLayout = GUICommon.ShowLayoutMenu( CurrentLayout, PreviousSelectedIndex );
          break;

        case ( (int)ContextMenuItem.SearchWithMpNZB ):
          string loadingParam = string.Format( "search:{0}", selectedFavoriteItem.Movie.Title );
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MpNZB, loadingParam );
          break;

        case ( (int)ContextMenuItem.SearchTorrent ):
          string loadPar = selectedFavoriteItem.Movie.Title;
          GUIWindowManager.ActivateWindow( (int)ExternalPluginWindows.MyTorrents, loadPar );
          break;

        default:
          break;
      }

      base.OnShowContextMenu();
    }

    #endregion

    #region Private Methods

    private void CheckAndPlayMovie( bool jumpTo )
    {
      var selectedItem = this.Facade.SelectedListItem as GUIMovieListItem;
      if ( selectedItem == null )
        return;

      var selectedMovie = selectedItem.Movie as TraktMovieSummary;
      GUICommon.CheckAndPlayMovie( jumpTo, selectedMovie );
    }

    private void LoadFavoriteMovies()
    {
      GUIUtils.SetProperty( "#Trakt.Items", string.Empty );

      GUIBackgroundTask.Instance.ExecuteInBackgroundAndCallback( () =>
      {
        return FavoriteMovies;
      },
      delegate ( bool success, object result )
      {
        if ( success )
        {
          var favorites = result as IEnumerable<TraktFavoriteItem>;
          SendFavoriteMoviesToFacade( favorites );
        }
      }, Translation.GettingFavorites, true );
    }

    private void SendFavoriteMoviesToFacade( IEnumerable<TraktFavoriteItem> movieFavorites )
    {
      // clear facade
      GUIControl.ClearControl( GetID, Facade.GetID );

      if ( movieFavorites == null )
      {
        GUIUtils.ShowNotifyDialog( Translation.Error, Translation.ErrorGeneral );
        GUIWindowManager.ShowPreviousWindow();
        return;
      }

      if ( movieFavorites.Count() == 0 )
      {
        GUIUtils.ShowNotifyDialog( GUIUtils.PluginName(), string.Format( Translation.NoMovieFavorites, CurrentUser ) );
        CurrentUser = TraktSettings.Username;
        GUIWindowManager.ShowPreviousWindow();
        return;
      }

      // sort movies
      var sortedList = movieFavorites.Where( m => !string.IsNullOrEmpty( m.Movie.Title ) ).ToList();
      sortedList.Sort( new GUIListItemMovieSorter( TraktSettings.SortByUserFavoriteMovies.Field, TraktSettings.SortByUserFavoriteMovies.Direction ) );

      int itemId = 0;
      var movieImages = new List<GUITmdbImage>();

      // Add each movie
      foreach ( var favoriteItem in sortedList )
      {
        // add image for download
        var images = new GUITmdbImage { MovieImages = new TmdbMovieImages { Id = favoriteItem.Movie.Ids.Tmdb } };
        movieImages.Add( images );

        var item = new GUIMovieListItem( favoriteItem.Movie.Title, (int)TraktGUIWindows.UserFavoriteMovies );

        item.Label2 = favoriteItem.Movie.Year == null ? "----" : favoriteItem.Movie.Year.ToString();
        item.TVTag = favoriteItem;
        item.Movie = favoriteItem.Movie;
        item.Images = images;
        item.ItemId = Int32.MaxValue - itemId;
        item.IsPlayed = favoriteItem.Movie.IsWatched();
        item.IconImage = GUIImageHandler.GetDefaultPoster( false );
        item.IconImageBig = GUIImageHandler.GetDefaultPoster();
        item.ThumbnailImage = GUIImageHandler.GetDefaultPoster();
        item.OnItemSelected += OnMovieSelected;
        Utils.SetDefaultIcons( item );
        Facade.Add( item );
        itemId++;
      }

      // Set Facade Layout
      Facade.CurrentLayout = CurrentLayout;
      GUIControl.FocusControl( GetID, Facade.GetID );

      if ( PreviousSelectedIndex >= movieFavorites.Count() )
        Facade.SelectIndex( PreviousSelectedIndex - 1 );
      else
        Facade.SelectIndex( PreviousSelectedIndex );

      // set facade properties
      GUIUtils.SetProperty( "#itemcount", movieFavorites.Count().ToString() );
      GUIUtils.SetProperty( "#Trakt.Items", string.Format( "{0} {1}", movieFavorites.Count().ToString(), movieFavorites.Count() > 1 ? Translation.Movies : Translation.Movie ) );

      // Download movie images Async and set to facade
      GUIMovieListItem.GetImages( movieImages );
    }

    private void InitProperties()
    {
      // Fanart
      backdrop.GUIImageOne = FanartBackground;
      backdrop.GUIImageTwo = FanartBackground2;
      backdrop.LoadingImage = loadingImage;

      // load Favorite movies for user
      if ( string.IsNullOrEmpty( CurrentUser ) )
        CurrentUser = TraktSettings.Username;
      GUICommon.SetProperty( "#Trakt.FavoriteMovies.CurrentUser", CurrentUser );

      // load last layout
      CurrentLayout = (GUIFacadeControl.Layout)TraktSettings.UserFavoriteMoviesDefaultLayout;

      // Update Button States
      UpdateButtonState();

      if ( sortButton != null )
      {
        sortButton.SortChanged += ( o, e ) =>
        {
          TraktSettings.SortByUserFavoriteMovies.Direction = (SortingDirections)( e.Order - 1 );
          PreviousSelectedIndex = 0;
          UpdateButtonState();
          LoadFavoriteMovies();
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
        sortButton.Label = GUICommon.GetSortByString( TraktSettings.SortByUserFavoriteMovies );
        sortButton.IsAscending = ( TraktSettings.SortByUserFavoriteMovies.Direction == SortingDirections.Ascending );
      }
      GUIUtils.SetProperty( "#Trakt.SortBy", GUICommon.GetSortByString( TraktSettings.SortByUserFavoriteMovies ) );
    }

    private void ClearProperties()
    {
      GUIUtils.SetProperty( "#Trakt.Movie.Favorite.Inserted", string.Empty );
      GUIUtils.SetProperty( "#Trakt.Movie.Favorite.Notes", string.Empty );
      GUICommon.ClearMovieProperties();
    }

    private void PublishFavoriteSkinProperties( TraktFavoriteItem item )
    {
      GUICommon.SetProperty( "#Trakt.Movie.Favorite.Inserted", item.ListedAt.FromISO8601().ToShortDateString() );
      GUICommon.SetProperty( "#Trakt.Movie.Favorite.Notes", item.Notes );
      GUICommon.SetMovieProperties( item.Movie );
    }

    private void OnMovieSelected( GUIListItem item, GUIControl parent )
    {
      PreviousSelectedIndex = Facade.SelectedListItemIndex;

      var favoriteItem = item.TVTag as TraktFavoriteItem;
      PublishFavoriteSkinProperties( favoriteItem );

      string fanart = TmdbCache.GetMovieBackdropFilename( ( item as GUIMovieListItem ).Images.MovieImages );
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