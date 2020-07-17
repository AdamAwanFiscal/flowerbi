import { QueryColumn } from "tinybi";

export const DateReported = {
    Id: new QueryColumn<Date>("DateReported.Id"),
    CalendarYearNumber: new QueryColumn<number>("DateReported.CalendarYearNumber"),
    FirstDayOfQuarter: new QueryColumn<Date>("DateReported.FirstDayOfQuarter"),
    FirstDayOfMonth: new QueryColumn<Date>("DateReported.FirstDayOfMonth"),
};

export const DateResolved = {
    Id: new QueryColumn<Date>("DateResolved.Id"),
    CalendarYearNumber: new QueryColumn<number>("DateResolved.CalendarYearNumber"),
    FirstDayOfQuarter: new QueryColumn<Date>("DateResolved.FirstDayOfQuarter"),
    FirstDayOfMonth: new QueryColumn<Date>("DateResolved.FirstDayOfMonth"),
};

export const DateAssigned = {
    Id: new QueryColumn<Date>("DateAssigned.Id"),
    CalendarYearNumber: new QueryColumn<number>("DateAssigned.CalendarYearNumber"),
    FirstDayOfQuarter: new QueryColumn<Date>("DateAssigned.FirstDayOfQuarter"),
    FirstDayOfMonth: new QueryColumn<Date>("DateAssigned.FirstDayOfMonth"),
};

export const Workflow = {
    Id: new QueryColumn<number>("Workflow.Id"),
    Resolved: new QueryColumn<boolean>("Workflow.Resolved"),
    WorkflowState: new QueryColumn<string>("Workflow.WorkflowState"),
    SourceOfError: new QueryColumn<string>("Workflow.SourceOfError"),
    FixedByCustomer: new QueryColumn<boolean>("Workflow.FixedByCustomer"),
};

export const Category = {
    Id: new QueryColumn<number>("Category.Id"),
    Label: new QueryColumn<string>("Category.Label"),
};

export const Customer = {
    Id: new QueryColumn<number>("Customer.Id"),
    CustomerName: new QueryColumn<string>("Customer.CustomerName"),
};

export const CoderAssigned = {
    Id: new QueryColumn<number>("CoderAssigned.Id"),
    FullName: new QueryColumn<string>("CoderAssigned.FullName"),
};

export const CoderResolved = {
    Id: new QueryColumn<number>("CoderResolved.Id"),
    FullName: new QueryColumn<string>("CoderResolved.FullName"),
};

export const CategoryCombination = {
    Id: new QueryColumn<number>("CategoryCombination.Id"),
    Crashed: new QueryColumn<boolean>("CategoryCombination.Crashed"),
    DataLoss: new QueryColumn<boolean>("CategoryCombination.DataLoss"),
    SecurityBreach: new QueryColumn<boolean>("CategoryCombination.SecurityBreach"),
    OffByOne: new QueryColumn<boolean>("CategoryCombination.OffByOne"),
    Slow: new QueryColumn<boolean>("CategoryCombination.Slow"),
    StackOverflow: new QueryColumn<boolean>("CategoryCombination.StackOverflow"),
};

export const Bug = {
    Id: new QueryColumn<number>("Bug.Id"),
    WorkflowId: new QueryColumn<number>("Bug.WorkflowId"),
    CustomerId: new QueryColumn<number>("Bug.CustomerId"),
    ReportedDate: new QueryColumn<Date>("Bug.ReportedDate"),
    ResolvedDate: new QueryColumn<Date>("Bug.ResolvedDate"),
    AssignedDate: new QueryColumn<Date>("Bug.AssignedDate"),
    CategoryCombinationId: new QueryColumn<number>("Bug.CategoryCombinationId"),
    AssignedCoderId: new QueryColumn<number>("Bug.AssignedCoderId"),
    ResolvedCoderId: new QueryColumn<number>("Bug.ResolvedCoderId"),
};

