/* Title:           Logon
 * Date:            3-4-17
 * Author:          Terry Holmes
 * 
 * Description:     This form is used to test the check logon */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VehicleHistoryDLL;
using VehiclesDLL;
using AutoSignInDLL;
using EventLogDLL;
using EmployeeDLL;
using DateSearchDLL;

namespace CheckingAutoLogIn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //setting up classes
        WPFMessagesClass TheMessagesClass = new WPFMessagesClass();
        VehicleHistoryClass TheVehicleHistoryClass = new VehicleHistoryClass();
        VehicleClass TheVehicleClass = new VehicleClass();
        AutoSignInClass TheAutoSignInClass = new AutoSignInClass();
        EventLogClass TheEventLogClass = new EventLogClass();
        EmployeeClass TheEmployeeClass = new EmployeeClass();
        DateSearchClass TheDateSearchClass = new DateSearchClass();

        VehiclesDataSet TheVehiclesDataSet;
        AutoSignInDateDataSet aAutoSignInDateDataSet;
        AutoSignInDateDataSetTableAdapters.autosignindateTableAdapter aAutoSignInDateTableAdapter;
        EmployeeDataSet TheEmployeeDataSet; 

        //setting the variables
        bool gblnRunRoutine;
        DateTime gdatTableDate;
        DateTime gdatTodaysDate;

        struct EmployeeStructure
        {
            public int mintEmployeeID;
            public string mstrLastName;
            public string mstrFirstName;
            public string mstrHomeOffice;
        }

        //variables for structure
        EmployeeStructure[] TheWarehouseStructure;
        int mintWarehouseCounter;
        int mintWarehouseUpperLimit;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TheVehiclesDataSet = TheVehicleClass.GetVehiclesInfo();

                dgvVehicles.ItemsSource = TheVehiclesDataSet.vehicles;
            }
            catch (Exception Ex)
            {
                TheEventLogClass.CreateEventLogEntry("Checking Auto Log In Main Window Window Loaded " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            TheMessagesClass.CloseTheProgram();
        }

        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            AutoSignInProcess();

            dgvVehicles.ItemsSource = TheVehiclesDataSet.vehicles;
        }
        public bool AutoSignInProcess()
        {
            bool blnFatalError = false;

            try
            {
                //loading the structure
                SetAutoSignInVariables();
                FillEmployeeStructure();
                blnFatalError = SignInVehicles();
                UpdateAutoSignInDateDB();

                //blnFatalError = mblnRunRoutine;
            }
            catch (Exception Ex)
            {
                TheEventLogClass.CreateEventLogEntry("Auto Sign In Date " + Ex.Message);

                blnFatalError = true;
            }

            return blnFatalError;
        }
        public bool SignInVehicles()
        {
            //setting local variables
            bool blnFatalError = false;
            int intVehicleCounter;
            int intVehicleNumberOfRecords;
            string strHomeOfficeForSearch;
            int intWarehouseCounter;
            int intWarehouseID = 0;
            string strRemoteVehicle;
            string strVehicleUnderRepair;
            string strActive;
            string strAvailable;
            int intVehicleID;
            int intBJCNumber;
            bool blnIsBoolean;
            DateTime datTodaysDate = DateTime.Now;
            DateTime datTransactionDate = DateTime.Now;


            try
            {
                if (gblnRunRoutine == true)
                {
                    //setting up the vehicles data set
                    TheVehiclesDataSet = TheVehicleClass.GetVehiclesInfo();
                    datTodaysDate = TheDateSearchClass.RemoveTime(datTodaysDate);

                    //getting the number of Variables
                    intVehicleNumberOfRecords = TheVehiclesDataSet.vehicles.Rows.Count - 1;

                    //beginning vehicle loop
                    for (intVehicleCounter = 0; intVehicleCounter <= intVehicleNumberOfRecords; intVehicleCounter++)
                    {
                        strActive = TheVehiclesDataSet.vehicles[intVehicleCounter].Active.ToUpper();
                        strAvailable = TheVehiclesDataSet.vehicles[intVehicleCounter].Available.ToUpper();
                        datTransactionDate = TheDateSearchClass.RemoveTime(TheVehiclesDataSet.vehicles[intVehicleCounter].Date);

                        //checking data for null
                        blnIsBoolean = TheVehiclesDataSet.vehicles[intVehicleCounter].IsOutOfTownNull();
                        if (blnIsBoolean == true)
                        {
                            strRemoteVehicle = "NO";
                        }
                        else
                        {
                            strRemoteVehicle = TheVehiclesDataSet.vehicles[intVehicleCounter].OutOfTown.ToUpper();
                        }

                        strVehicleUnderRepair = TheVehiclesDataSet.vehicles[intVehicleCounter].DownForRepairs.ToUpper();
                        strHomeOfficeForSearch = TheVehiclesDataSet.vehicles[intVehicleCounter].HomeOffice.ToUpper();
                        intVehicleID = TheVehiclesDataSet.vehicles[intVehicleCounter].VehicleID;
                        intBJCNumber = TheVehiclesDataSet.vehicles[intVehicleCounter].BJCNumber;

                        //if statements
                        if (strActive == "YES")
                        {
                            if (strAvailable == "NO")
                            {
                                if (strRemoteVehicle == "NO")
                                {
                                    if(datTransactionDate < datTodaysDate)
                                    {
                                        //updating the variables
                                        TheVehiclesDataSet.vehicles[intVehicleCounter].Available = "YES";
                                        TheVehiclesDataSet.vehicles[intVehicleCounter].Date = DateTime.Now;

                                        for (intWarehouseCounter = 0; intWarehouseCounter <= mintWarehouseUpperLimit; intWarehouseCounter++)
                                        {
                                            if (strHomeOfficeForSearch == TheWarehouseStructure[intWarehouseCounter].mstrHomeOffice)
                                            {
                                                intWarehouseID = TheWarehouseStructure[intWarehouseCounter].mintEmployeeID;
                                                TheVehiclesDataSet.vehicles[intVehicleCounter].EmployeeID = intWarehouseID;
                                            }
                                        }

                                        TheVehicleClass.UpdateVehiclesDB(TheVehiclesDataSet);
                                        TheVehicleHistoryClass.CreateVehicleHistoryTransaction(intVehicleID, intBJCNumber, intWarehouseID, intWarehouseID, "AUTO LOG IN", "NO");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                TheEventLogClass.CreateEventLogEntry("Auto Sign In Vehicles " + Ex.Message);

                blnFatalError = true;
            }

            return blnFatalError;
        }

        private void SetAutoSignInVariables()
        {
            try
            {
                //setting up the data
                aAutoSignInDateDataSet = new AutoSignInDateDataSet();
                aAutoSignInDateTableAdapter = new AutoSignInDateDataSetTableAdapters.autosignindateTableAdapter();
                aAutoSignInDateTableAdapter.Fill(aAutoSignInDateDataSet.autosignindate);

                //getting the table date
                gdatTableDate = aAutoSignInDateDataSet.autosignindate[0].AutoSignInDate;
                gdatTableDate = TheDateSearchClass.RemoveTime(gdatTableDate);
                gdatTodaysDate = DateTime.Now;
                gdatTodaysDate = TheDateSearchClass.RemoveTime(gdatTodaysDate);

                if (gdatTodaysDate > gdatTableDate)
                {
                    gblnRunRoutine = true;
                }
                else
                {
                    gblnRunRoutine = true;
                }

            }
            catch (Exception Ex)
            {
                TheEventLogClass.CreateEventLogEntry("Auto Sign In Class " + Ex.Message);
            }

        }
        private void UpdateAutoSignInDateDB()
        {
            try
            {
                aAutoSignInDateDataSet = new AutoSignInDateDataSet();
                aAutoSignInDateTableAdapter = new AutoSignInDateDataSetTableAdapters.autosignindateTableAdapter();
                aAutoSignInDateTableAdapter.Fill(aAutoSignInDateDataSet.autosignindate);

                aAutoSignInDateDataSet.autosignindate[0].AutoSignInDate = gdatTodaysDate;
                aAutoSignInDateTableAdapter.Update(aAutoSignInDateDataSet.autosignindate);
            }
            catch (Exception Ex)
            {
                TheEventLogClass.CreateEventLogEntry("Updating Auto Sign In DB " + Ex.Message);
            }
        }

        //Creating method to fill employee structure
        private void FillEmployeeStructure()
        {
            //set local variables
            int intCounter;
            int intNumberOfRecords;
            string strLastNameForSearch;
            string strLastNameFromTable;

            try
            {
                //loading the data set
                TheEmployeeDataSet = TheEmployeeClass.GetEmployeeInfo();

                //getting ready for the loop
                intNumberOfRecords = TheEmployeeDataSet.employees.Rows.Count - 1;
                TheWarehouseStructure = new EmployeeStructure[intNumberOfRecords + 1];
                mintWarehouseCounter = 0;
                strLastNameForSearch = "WAREHOUSE";

                //beginning loop
                for (intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    //checking to see if the record is active
                    strLastNameFromTable = TheEmployeeDataSet.employees[intCounter].LastName.ToUpper();


                    if (strLastNameForSearch == strLastNameFromTable)
                    {
                        //loading up the warehouse structure
                        TheWarehouseStructure[mintWarehouseCounter].mintEmployeeID = TheEmployeeDataSet.employees[intCounter].EmployeeID;
                        TheWarehouseStructure[mintWarehouseCounter].mstrFirstName = TheEmployeeDataSet.employees[intCounter].FirstName.ToUpper();
                        TheWarehouseStructure[mintWarehouseCounter].mstrLastName = strLastNameFromTable;
                        TheWarehouseStructure[mintWarehouseCounter].mstrHomeOffice = TheEmployeeDataSet.employees[intCounter].HomeOffice.ToUpper();
                        mintWarehouseUpperLimit = mintWarehouseCounter;
                        mintWarehouseCounter++;
                    }

                }

                //setting the counter
                mintWarehouseCounter = 0;
            }
            catch (Exception Ex)
            {
                TheEventLogClass.CreateEventLogEntry("The Auto Sign In Date " + Ex.Message);
            }
        }
    }
}
