﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using VkNet.Model;
using Grats.ViewModels;
using Grats.Model;
using Windows.UI;
using static Grats.EditorPage;
using Windows.UI.Xaml.Media.Animation;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace Grats
{
    /// <summary>
    /// Сводная страница. Появляется первой на главной странице.
    /// Содержит календарь и историю поздравлений.
    /// </summary>
    public sealed partial class SummaryPage : Page
    {
        public List<EventCalendarView> CalendarEvents = new List<EventCalendarView>();
        Point pointerPosition = new Point();
        public SummaryPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            var friends = e.Parameter as List<User>;
            UpdateCalendar(friends);
            UpdateHistory();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

        }

        #region Обновление секций

        private void UpdateHistory()
        {
            // TODO: Реализовать
        }

        private void UpdateCalendar(List<User> friends)
        {
            UpdateEvents(friends);
        }
        public async void UpdateEvents(List<User> friends)
        {
            var db = (App.Current as App).dbContext;
            var categories = Enumerable.Union<Category>(
                db.BirthdayCategories,
                db.GeneralCategories);
            //добавление событий
            foreach (var category in categories)
            {
                var colorFromName = AngleSharp.Css.Values.Color.FromName(category.Color).Value;
                var color = Color.FromArgb(colorFromName.A, colorFromName.R, colorFromName.G, colorFromName.B);
                CalendarEvents.AddRange(category?.Tasks?.Select(task => new EventCalendarView { EventColor = color, EventDate = task.DispatchDate, Contacts = category, EventName = category.Name }));
            }
            //добавление дней рождений
            CalendarEvents.AddRange(db?.Contacts?.Select(contact => new EventCalendarView { EventColor = Colors.LightSkyBlue, EventDate = contact.Birthday.Value }));
            foreach (var friend in friends)
            {
                BirthdayCategory cat = new BirthdayCategory();
                cat.Color = "#00FF00FF";
                cat.Name = "День рождения пользователя " + friend.FirstName + " " + friend.LastName;
                cat.CategoryContacts = new List<CategoryContact>() { new CategoryContact { Category = cat, Contact = new Model.Contact(friend) } };
                try
                {
                    CalendarEvents.Add(new EventCalendarView { EventColor = Colors.LightSkyBlue, EventDate = DateTime.Parse(friend.BirthDate), Contacts = cat, EventName = cat.Name });
                }
                catch { }
            }
        }
        private void CalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            // Render basic day items.
            if (args.Phase == 0)
            {
                // Register callback for next phase.
                args.RegisterUpdateCallback(CalendarView_CalendarViewDayItemChanging);
            }
            // Устанавливаем даты, которые затемняем.
            else if (args.Phase == 1)
            {
                // Затемняем прошедшие даты
                if (args.Item.Date < DateTimeOffset.Now)
                {
                    args.Item.IsBlackout = true;
                }
                // Register callback for next phase.
                args.RegisterUpdateCallback(CalendarView_CalendarViewDayItemChanging);
            }
            // Set density bars.
            else if (args.Phase == 2)
            {
                // Avoid unnecessary processing.
                // Не нужны даты, которые были до нынешней
                if (args.Item.Date >= DateTimeOffset.Now)
                {
                    // Get bookings for the date being rendered.

                    List<Color> densityColors = new List<Color>();
                    // Set a density bar color for each of the days bookings.
                    // It's assumed that there can't be more than 10 bookings in a day. Otherwise,
                    // further processing is needed to fit within the max of 10 density bars.
                    foreach (var calendarEvent in CalendarEvents)
                    {
                        if (calendarEvent.EventDate.Day == args.Item.Date.Day && calendarEvent.EventDate.Month == args.Item.Date.Month)
                            densityColors.Add(calendarEvent.EventColor);
                    }
                    if (densityColors.Count > 0)
                        args.Item.SetDensityColors(FillColors(densityColors));
                }
            }
        }
        public List<Color> FillColors(List<Color> densityColors)
        {
            List<Color> colors = new List<Color>();
            foreach (var densityColor in densityColors)
                for (int i = 0; i < 10.0 / densityColors.Count; i++)
                    colors.Add(densityColor);
            return colors;
        }
        private void calendar_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            var calendar = ((CalendarView)sender);
            var selectedDate = args.AddedDates?.FirstOrDefault();
            if (selectedDate == null || !selectedDate.HasValue)
                return;
            var eventsOfDay = CalendarEvents?.Where(x => x.EventDate.Day == selectedDate.Value.Day && x.EventDate.Month == selectedDate.Value.Month);
            if (eventsOfDay != null && eventsOfDay.Count() > 0)
            {
                var flyout = new MenuFlyout();
                calendar.ContextFlyout = flyout;
                foreach (var item in eventsOfDay)
                {
                    var flyoutItem = new MenuFlyoutItem();
                    flyoutItem.DataContext = item;
                    flyoutItem.Text = item.EventName;
                    flyoutItem.Click += MenuFlyoutItem_Click;
                    flyoutItem.Foreground = new SolidColorBrush(item.EventColor);
                    flyout.Items.Add(flyoutItem);
                }
                flyout.ShowAt(this, pointerPosition);
            }
        }
        #endregion


        private void calendar_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            pointerPosition = e.GetCurrentPoint(this).Position;
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var eventDesc = (EventCalendarView)((MenuFlyoutItem)sender).DataContext;
            var MainFrame = (Frame)this.Parent;
            MainFrame.Navigate(
                typeof(EditorPage),
                new NewCategoryParameter()
                {
                    Category = eventDesc.Contacts
                },
                new DrillInNavigationTransitionInfo());
        }
    }
}
