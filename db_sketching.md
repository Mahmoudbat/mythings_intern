
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
    user_id refrences the user on column id  -- if it was for a user
    store_id refrences the store id -- if it was for a store
    name varchar not null
    city varchar not null
    area not null
    street not null,
    building_number varchar not null
    floor int not null
    note varchar -- optional
    lattitude decimal not null
    longtatuide decimal not null
    created_at defaults to current timestamp
    updated_at defaults to current timestamp
    CHECK ((user_id IS NULL) <> (store_id IS NULL)) -- cause a store and a user might have same id.... due to bad inserting 
)

alter table users on column last_used_address to refrence address address_id 

create table store (

    id uuid primary key
    name varchar not null unique 
    owner_id refrences user id table not null -- imma assume that a person could own multiple stores 
    is_paused BOOLEAN defaults to false not null
    -- i could know if it has more than one item for sale by selecting from products table where stre = store_id so no need to add a var here
    catagory varchar not null check in(restaurant, groceries, cafe)
    opens_at TIME not null
    closes_at TIME not null
    check( opens_at <  closes_at)
    delivery_fee decimal
    created_at 
    updated_at
)

-- here i could create a a josn object in the store for menu or make its own seperate table im not sure....i prob should do this one in a seperate table since it has section, product, price, quantity, options for each item, like (size, extras, etc) each with price (DECIMAL(10,3).)addition #todo

-- i can do the same for sections too.... i can do json object in store table or a seperate table.... ex (burgers, sandwhiches , sides, drinks) this one should be in a seperate table too since it will be refrenced in product to see what section the product is in... but tat means i have to complex sql queres aka cte to get the section ide when inserting a new item to menu... tho thats just initial thoughts... #todo 










