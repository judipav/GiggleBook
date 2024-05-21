--
-- NOTE:
--
-- File paths need to be edited. Search for $$PATH$$ and
-- replace it with the path to the directory containing
-- the extracted data files.
--
--
-- PostgreSQL database dump
--

-- Dumped from database version 15.6 (Debian 15.6-1.pgdg120+2)
-- Dumped by pg_dump version 16.0

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

DROP DATABASE IF EXISTS gigglebook;
--
-- Name: gigglebook; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE gigglebook WITH TEMPLATE = template0 ENCODING = 'UTF8' ICU_LOCALE = 'ru-RU' LOCALE_PROVIDER = 'icu';


ALTER DATABASE gigglebook OWNER TO postgres;

\connect gigglebook

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: gender; Type: DOMAIN; Schema: public; Owner: postgres
--

CREATE DOMAIN public.gender AS character(1)
	CONSTRAINT gender_check CHECK ((VALUE = ANY (ARRAY['F'::bpchar, 'M'::bpchar])));


ALTER DOMAIN public.gender OWNER TO postgres;

--
-- Name: add_friend(uuid, uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.add_friend(current_user_id uuid, friend_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$

declare cnt integer;
begin
	select count(*) into cnt from "Friends" f
	where f.user_id = current_user_id and f.friend_id = friend_user_id;
	
	if (cnt) > 0 then 
		raise exception 'Уже в друзьях';
	end if;
	
	INSERT INTO public."Friends"(
	user_id, friend_id)
	VALUES (current_user_id, friend_user_id);
	
	return true;
end;
$$;


ALTER FUNCTION public.add_friend(current_user_id uuid, friend_user_id uuid) OWNER TO postgres;

--
-- Name: auth_user(character, character); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.auth_user(user_name character, hash character) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.username = user_name and u.password = hash;
end;
$$;


ALTER FUNCTION public.auth_user(user_name character, hash character) OWNER TO postgres;

--
-- Name: find_user(character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.find_user(user_fname character varying, user_sname character varying) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query 
	select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
		where 	to_tsvector('russian', u.first_name) @@ to_tsquery('user_fname') 
			AND to_tsvector('russian', u.second_name) @@ to_tsquery('user_sname')
		order by id;
end;
$$;


ALTER FUNCTION public.find_user(user_fname character varying, user_sname character varying) OWNER TO postgres;

--
-- Name: get_user(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_user(user_id uuid) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.id = user_id;
end;
$$;


ALTER FUNCTION public.get_user(user_id uuid) OWNER TO postgres;

--
-- Name: list_message(uuid, uuid, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.list_message(from_uid uuid, to_uid uuid, count_skip integer, take integer) RETURNS TABLE(id uuid, friend_id uuid, msg_text character, datesend date)
    LANGUAGE plpgsql
    AS $$
begin
	return query SELECT d.from, d.to, d.text, d.datesend
	FROM "DialogMessage" d
	where d.from = from_uid and d.to = to_uid
	ORDER BY datesend DESC
	OFFSET (@count_skip) ROWS FETCH NEXT (@Take) ROWS ONLY;
end;
$$;


ALTER FUNCTION public.list_message(from_uid uuid, to_uid uuid, count_skip integer, take integer) OWNER TO postgres;

--
-- Name: post_create(uuid, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_create(author_uid uuid, post_text text) RETURNS TABLE(post_id uuid)
    LANGUAGE plpgsql
    AS $$
begin
	return query INSERT INTO public."Post"( 
	"text", author_user_id)
	VALUES (post_text, author_uid)
	RETURNING id;
	
end;
$$;


ALTER FUNCTION public.post_create(author_uid uuid, post_text text) OWNER TO postgres;

--
-- Name: post_delete(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_delete(post_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Post" p
	where p.user_id = post_id;
	
	if (cnt) = 0 then 
		raise exception 'Пост не найден';
	end if;
	
	delete from public."Post"
	WHERE "id" = post_id;
		
	return true;
end;
$$;


ALTER FUNCTION public.post_delete(post_id uuid) OWNER TO postgres;

--
-- Name: post_feed(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_feed(r_user_id uuid) RETURNS TABLE(post_id uuid, author_id uuid, post_text text)
    LANGUAGE plpgsql
    AS $$

begin
	RETURN QUERY SELECT p."id", p.author_user_id, p.text
	FROM "Post" p
	LEFT JOIN "Friends" f on f.user_id = r_user_id
	WHERE f.friend_id = p.author_user_id
	or p.author_user_id = r_user_id;
end;
$$;


ALTER FUNCTION public.post_feed(r_user_id uuid) OWNER TO postgres;

--
-- Name: post_get(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_get(post_id uuid) RETURNS TABLE(post_text text)
    LANGUAGE plpgsql
    AS $$

begin
	return query select "text" from public."Post" where "id" = post_id;
end;
$$;


ALTER FUNCTION public.post_get(post_id uuid) OWNER TO postgres;

--
-- Name: post_update(uuid, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_update(post_id uuid, post_text text) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Post" p
	where p.user_id = post_id;
	
	if (cnt) = 0 then 
		raise exception 'Пост не найден';
	end if;

	UPDATE public."Post"
	SET "text" = post_text
	WHERE "id" = post_id;
		
	return true;
end;
$$;


ALTER FUNCTION public.post_update(post_id uuid, post_text text) OWNER TO postgres;

--
-- Name: register_user(character, character, date, character, character, character, character, character); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.register_user(f_name character, s_name character, dt_birth date, bio character, city character, sword character, u_name character, u_sex character) RETURNS uuid
    LANGUAGE plpgsql
    AS $$
declare w int;
begin
	select count(*) into w from "User" u
		where u.username = u_name ;
	
	if w > 0 then 
		RAISE EXCEPTION 'login уже существуе';
	end if;
	
	INSERT INTO public."User"(
	first_name, second_name, birthdate, biography, city, password, username, sex)
	VALUES (f_name, s_name, dt_birth, bio, city, sword, u_name, u_sex)
	RETURNING id INTO res;
		
	return res; 
end;	
$$;


ALTER FUNCTION public.register_user(f_name character, s_name character, dt_birth date, bio character, city character, sword character, u_name character, u_sex character) OWNER TO postgres;

--
-- Name: remove_friend(uuid, uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.remove_friend(current_user_id uuid, friend_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Friends" f
	where f.user_id = current_user_id and f.friend_id = friend_user_id;
	
	if (cnt) > 0 then 
		DELETE FROM public."Friends" f
		where f.user_id = current_user_id and friend_id = friend_user_id;
		
		return true;
	end if;
end;
$$;


ALTER FUNCTION public.remove_friend(current_user_id uuid, friend_user_id uuid) OWNER TO postgres;

--
-- Name: send_message(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.send_message(from_uid uuid, to_uid uuid, msg character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "User" u
	where u.id in (from_uid, to_uid);
	
	if (cnt) <> 2 then
		raise exception 'Пользователя не существует';
	end if;
	
	INSERT INTO public."DialogMessage"(
	"from", "to", text)
	VALUES (from_uid, to_uid, msg);
	
	return true;
end;
$$;


ALTER FUNCTION public.send_message(from_uid uuid, to_uid uuid, msg character varying) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: DialogMessage; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DialogMessage" (
    "from" uuid NOT NULL,
    "to" uuid NOT NULL,
    text text NOT NULL,
    datesend timestamp with time zone DEFAULT now()
);


ALTER TABLE public."DialogMessage" OWNER TO postgres;

--
-- Name: Friends; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Friends" (
    user_id uuid NOT NULL,
    friend_id uuid NOT NULL
);


ALTER TABLE public."Friends" OWNER TO postgres;

--
-- Name: Post; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Post" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    text text NOT NULL,
    author_user_id uuid NOT NULL
);


ALTER TABLE public."Post" OWNER TO postgres;

--
-- Name: User; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."User" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    first_name character varying(100) NOT NULL,
    second_name character varying(100) NOT NULL,
    birthdate date NOT NULL,
    biography character varying,
    city character varying NOT NULL,
    password character varying NOT NULL,
    username character varying,
    sex public.gender
);


ALTER TABLE public."User" OWNER TO postgres;

--
-- Name: cnt; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.cnt (
    count bigint
);


ALTER TABLE public.cnt OWNER TO postgres;

--
-- Data for Name: DialogMessage; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."DialogMessage" ("from", "to", text, datesend) FROM stdin;
\.
COPY public."DialogMessage" ("from", "to", text, datesend) FROM '/docker-entrypoint-initdb.d/3390.dat';

--
-- Data for Name: Friends; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Friends" (user_id, friend_id) FROM stdin;
\.
COPY public."Friends" (user_id, friend_id) FROM '/docker-entrypoint-initdb.d/3391.dat';

--
-- Data for Name: Post; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Post" (id, text, author_user_id) FROM stdin;
\.
COPY public."Post" (id, text, author_user_id) FROM '/docker-entrypoint-initdb.d/3392.dat';

--
-- Data for Name: User; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."User" (id, first_name, second_name, birthdate, biography, city, password, username, sex) FROM stdin;
\.
COPY public."User" (id, first_name, second_name, birthdate, biography, city, password, username, sex) FROM '/docker-entrypoint-initdb.d/3393.dat';

--
-- Data for Name: cnt; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.cnt (count) FROM stdin;
\.
COPY public.cnt (count) FROM '/docker-entrypoint-initdb.d/3394.dat';

--
-- Name: Friends friends_user_id_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT friends_user_id_pkey PRIMARY KEY (user_id);


--
-- Name: User pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT pkey PRIMARY KEY (id);


--
-- Name: Post post_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Post"
    ADD CONSTRAINT post_pkey PRIMARY KEY (id);


--
-- Name: Friends unique_friends; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT unique_friends UNIQUE (user_id, friend_id);


--
-- Name: User username_loginname; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT username_loginname UNIQUE (username);


--
-- Name: DialogMessage dialog_from_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DialogMessage"
    ADD CONSTRAINT dialog_from_user_id_fkey FOREIGN KEY ("from") REFERENCES public."User"(id) ON DELETE CASCADE;


--
-- Name: DialogMessage dialog_to_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DialogMessage"
    ADD CONSTRAINT dialog_to_user_id_fkey FOREIGN KEY ("to") REFERENCES public."User"(id);


--
-- Name: Friends friend_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT friend_id_fkey FOREIGN KEY (friend_id) REFERENCES public."User"(id);


--
-- Name: Post post_author_uid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Post"
    ADD CONSTRAINT post_author_uid FOREIGN KEY (author_user_id) REFERENCES public."User"(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

