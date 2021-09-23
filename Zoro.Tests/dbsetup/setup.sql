/*
create TABLE testdata
(
    name NVARCHAR(100) not NULL,
    iban NVARCHAR(50) not NULL,
    country NVARCHAR(50) not NULL,
    address NVARCHAR(100) not NULL
);
go
*/
/*
insert into testdata
(name, iban, country, address)
VALUES
('Logistics ZH','CH9880808007645910141','CH','Limmatgasse 99');
insert into testdata
(name, iban, country, address)
VALUES
('Metafores Patras','GR4701102050000020547061026','GR','Agiou Andrea 3');
insert into testdata
(name, iban, country, address)
VALUES
('Transporto di Roma','IT36K0890150920000000550061','IT','Via Milano 5');
go
*/
select * from testdata;