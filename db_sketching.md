
-- imma assume backend is using rest apis basically stateless so imma save values between change i think that would be unnesscary if it has an open connection till the user finishes requests but thats unlikley 

create table users ( -- while its preffered to use words that repreesent one thing user is a reserverd word so its users 
    
    id uuid primary key
    first_name not null
    last_name noy null
    phone_num unique not null
    gender checks in (male, female) not null
    last_used_address  -- will alter table later to refrence addres on addres_id becuase of refrence loop so the app automatically pick the last used location or could be edited to the most used location ? according to bussnies desecions 
    created_at defaults to current time 
    updated_at defaults to current time
)
-- im not gonna be adding anything here to check if a user has an order in some catagory since that could be known using select on orders tables

-- adress have diff table customer can have more than one location saved

-- i should add on delete cascade here somewhere so when user delete their account all of thier adresses gets deleted too... maybe i should add to more tables too 

create table address (
    id uuid primary key
    user_id refrences the user on column id  on DELETE CASCADE-- if it was for a user
    name varchar not null
    city varchar not null
    area not null
    street not null,
    building_number varchar not null
    floor int not null
    note varchar -- optional
    lattitude DECIMAL(9,6) not null
    longtatuide DECIMAL(9,6) not null
    created_at defaults to current timestamp
    updated_at defaults to current timestamp
)

alter table users on column last_used_address to refrence address address_id 

-- sql server can sort by distance 

create table store (

    id uuid primary key
    name varchar not null 
    owner_id refrences user id table not null -- imma assume that a person could own multiple stores 
    is_paused BOOLEAN defaults to false not null
    -- i could know if it has more than one item for sale by selecting from products table where stre = store_id so no need to add a var here
    catagory varchar not null check in(restaurant, groceries, cafe)
    opens_at TIME not null
    closes_at TIME not null
    delivery_fee DECIMAL(10,3)
    lattitude DECIMAL(9,6) not null
    city
    street_name
    longtatuide DECIMAL(9,6) not null 
    created_at 
    updated_at
) -- location doesnt refrence address table to optimize space

create table section (
    id uuid pk
    name varchar not null
    store_id refrence store on id not null
    sort_order int 
    created_at
    updated_at
)

create table menu_item (
    id uuid pk
    name varchar not null
    description text
    section_id refrence section on id not null
    price DECIMAL(10,3) not null
    is_sold_out boolen default false not null
    deleted_at timestamp defaults null
    created_at
    updated_at

)

create table option_group (
    id uuid pk
    name varchar not null
    store_id refrence store on id not null
    min_select int not null
    max_select int not null
    CHECK (min_select >= 0 AND max_select >= min_select)
)

create table menu_option (
    id pk uuid
    name not null
    option_group_id refrence option_group on id not null
    extra_price DECIMAL(10,3) not null
)

create table menu_item_option_group (
    id uuid pk
    menu_item_id refrence menu_item on id
    option_group_id also refrence
    UNIQUE (menu_item_id, option_group_id) 
)


-- todo order by location closer comes first







